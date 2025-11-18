package sandbox

import (
	"bytes"
	"context"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"time"

	"execution_service/internal/config"
	"execution_service/internal/models"
)

type IsolateSandbox struct {
	config *config.IsolateConfig
}

type ExecutionResult struct {
	Verdict       models.Verdict
	Output        string
	Error         string
	ExecutionTime int
	MemoryUsed    int
	ExitCode      int
}

type CompileResult struct {
	Success bool
	Output  string
	Error   string
}

func NewIsolateSandbox(cfg *config.IsolateConfig) *IsolateSandbox {
	return &IsolateSandbox{
		config: cfg,
	}
}

func (i *IsolateSandbox) Compile(ctx context.Context, language string, code []byte, timeLimit time.Duration) (*CompileResult, error) {
	boxID, err := i.createBox()
	if err != nil {
		return nil, fmt.Errorf("failed to create isolate box: %w", err)
	}
	defer i.cleanupBox(boxID)

	boxDir := i.getBoxDir(boxID)
	codeFile := filepath.Join(boxDir, "code"+getFileExtension(language))

	err = os.WriteFile(codeFile, code, 0644)
	if err != nil {
		return nil, fmt.Errorf("failed to write code file: %w", err)
	}

	langConfig := getLanguageConfig(language)
	if langConfig.CompileCommand == nil {
		return &CompileResult{Success: true}, nil
	}

	compileCmd := strings.ReplaceAll(*langConfig.CompileCommand, "{input}", "code"+getFileExtension(language))
	compileCmd = strings.ReplaceAll(compileCmd, "{output}", "program")

	args := []string{
		"--box-id=" + strconv.Itoa(boxID),
		"--cg",
		"--cg-timing",
		"--processes=10",
		"--mem=262144",
		"--time=" + strconv.Itoa(int(timeLimit.Seconds())),
		"--wall-time=" + strconv.Itoa(int(timeLimit.Seconds()*2)),
		"--fsize=16384",
		"--env=PATH=/usr/bin:/bin",
		"--dir=/etc:noexec",
		"--dir=/usr:noexec",
		"--dir=/lib:noexec",
		"--dir=/lib64:noexec",
		"--run",
		"--",
		"/bin/bash",
		"-c",
		compileCmd,
	}

	cmd := exec.CommandContext(ctx, i.config.Path, args...)
	cmd.Dir = boxDir

	var stdout, stderr bytes.Buffer
	cmd.Stdout = &stdout
	cmd.Stderr = &stderr

	err = cmd.Run()
	if err != nil {
		if _, ok := err.(*exec.ExitError); ok {
			return &CompileResult{
				Success: false,
				Output:  stdout.String(),
				Error:   stderr.String(),
			}, nil
		}
		return nil, fmt.Errorf("failed to run compile command: %w", err)
	}

	return &CompileResult{
		Success: true,
		Output:  stdout.String(),
		Error:   stderr.String(),
	}, nil
}

func (i *IsolateSandbox) Execute(ctx context.Context, language string, input []byte, timeLimit time.Duration, memoryLimit int) (*ExecutionResult, error) {
	boxID, err := i.createBox()
	if err != nil {
		return nil, fmt.Errorf("failed to create isolate box: %w", err)
	}
	defer i.cleanupBox(boxID)

	boxDir := i.getBoxDir(boxID)
	inputFile := filepath.Join(boxDir, "input.txt")

	err = os.WriteFile(inputFile, input, 0644)
	if err != nil {
		return nil, fmt.Errorf("failed to write input file: %w", err)
	}

	langConfig := getLanguageConfig(language)
	runCmd := strings.ReplaceAll(langConfig.ExecuteCommand, "{executable}", "program")
	runCmd = strings.ReplaceAll(runCmd, "{input}", "code"+getFileExtension(language))
	runCmd = strings.ReplaceAll(runCmd, "{classname}", "Main")

	args := []string{
		"--box-id=" + strconv.Itoa(boxID),
		"--cg",
		"--cg-timing",
		"--processes=1",
		"--mem=" + strconv.Itoa(memoryLimit),
		"--time=" + strconv.Itoa(int(timeLimit.Seconds())),
		"--wall-time=" + strconv.Itoa(int(timeLimit.Seconds()*2)),
		"--extra-time=0.5",
		"--stack=65536",
		"--fsize=16384",
		"--chdir=/box",
		"--env=HOME=/tmp",
		"--env=PATH=/usr/bin:/bin",
		"--dir=/etc:noexec",
		"--dir=/usr:noexec",
		"--dir=/lib:noexec",
		"--dir=/lib64:noexec",
		"--stdin=input.txt",
		"--stdout=output.txt",
		"--stderr=error.txt",
		"--run",
		"--",
		"/bin/bash",
		"-c",
		runCmd,
	}

	cmd := exec.CommandContext(ctx, i.config.Path, args...)
	cmd.Dir = boxDir

	err = cmd.Run()
	if err != nil {
		if exitErr, ok := err.(*exec.ExitError); ok {
			return i.parseExecutionResult(boxID, exitErr.ExitCode())
		}
		return nil, fmt.Errorf("failed to run execute command: %w", err)
	}

	return i.parseExecutionResult(boxID, 0)
}

func (i *IsolateSandbox) parseExecutionResult(boxID int, exitCode int) (*ExecutionResult, error) {
	boxDir := i.getBoxDir(boxID)

	outputFile := filepath.Join(boxDir, "output.txt")
	errorFile := filepath.Join(boxDir, "error.txt")
	metaFile := filepath.Join(boxDir, "meta.txt")

	output, _ := os.ReadFile(outputFile)
	errorStr, _ := os.ReadFile(errorFile)
	meta, _ := os.ReadFile(metaFile)

	result := &ExecutionResult{
		Output:   string(output),
		Error:    string(errorStr),
		ExitCode: exitCode,
	}

	result.ExecutionTime, result.MemoryUsed = i.parseMetaFile(string(meta))

	result.Verdict = i.determineVerdict(exitCode, result.ExecutionTime, result.MemoryUsed)

	return result, nil
}

func (i *IsolateSandbox) parseMetaFile(meta string) (timeMs, memoryKb int) {
	lines := strings.Split(meta, "\n")
	for _, line := range lines {
		if strings.HasPrefix(line, "time:") {
			timeStr := strings.TrimSpace(strings.TrimPrefix(line, "time:"))
			if time, err := strconv.ParseFloat(timeStr, 64); err == nil {
				timeMs = int(time * 1000)
			}
		}
		if strings.HasPrefix(line, "max-rss:") {
			memStr := strings.TrimSpace(strings.TrimPrefix(line, "max-rss:"))
			if mem, err := strconv.Atoi(memStr); err == nil {
				memoryKb = mem / 1024
			}
		}
	}
	return
}

func (i *IsolateSandbox) determineVerdict(exitCode, timeMs, memoryKb int) models.Verdict {
	if exitCode != 0 {
		if timeMs > 0 && timeMs < 100 {
			return models.VerdictRuntime
		}
		return models.VerdictRuntime
	}

	if timeMs > 5000 {
		return models.VerdictTimeLim
	}

	if memoryKb > 262144 {
		return models.VerdictMemLim
	}

	return models.VerdictAccepted
}

func (i *IsolateSandbox) createBox() (int, error) {
	cmd := exec.Command(i.config.Path, "--init")
	output, err := cmd.CombinedOutput()
	if err != nil {
		return 0, fmt.Errorf("failed to initialize isolate box: %w, output: %s", err, string(output))
	}

	boxIDStr := strings.TrimSpace(string(output))
	boxID, err := strconv.Atoi(boxIDStr)
	if err != nil {
		return 0, fmt.Errorf("failed to parse box ID: %w", err)
	}

	return boxID, nil
}

func (i *IsolateSandbox) cleanupBox(boxID int) {
	cmd := exec.Command(i.config.Path, "--box-id="+strconv.Itoa(boxID), "--cleanup")
	cmd.Run()
}

func (i *IsolateSandbox) getBoxDir(boxID int) string {
	return filepath.Join(i.config.BoxRoot, fmt.Sprintf("%d", boxID))
}

func (i *IsolateSandbox) CleanupAll() error {
	cmd := exec.Command(i.config.Path, "--cleanup")
	return cmd.Run()
}

func getLanguageConfig(language string) models.SupportedLanguage {
	configs := map[string]models.SupportedLanguage{
		"cpp": {
			CompileCommand: stringPtr("g++ -O2 -std=c++17 -o program code.cpp"),
			ExecuteCommand: "./program",
		},
		"c": {
			CompileCommand: stringPtr("gcc -O2 -std=c11 -o program code.c"),
			ExecuteCommand: "./program",
		},
		"java": {
			CompileCommand: stringPtr("javac code.java"),
			ExecuteCommand: "java Main",
		},
		"python": {
			CompileCommand: nil,
			ExecuteCommand: "python3 code.py",
		},
		"go": {
			CompileCommand: stringPtr("go build -o program code.go"),
			ExecuteCommand: "./program",
		},
	}

	if config, exists := configs[language]; exists {
		return config
	}

	return models.SupportedLanguage{
		CompileCommand: nil,
		ExecuteCommand: "python3 code.py",
	}
}

func getFileExtension(language string) string {
	extensions := map[string]string{
		"cpp":    ".cpp",
		"c":      ".c",
		"java":   ".java",
		"python": ".py",
		"go":     ".go",
	}

	if ext, exists := extensions[language]; exists {
		return ext
	}
	return ".txt"
}

func stringPtr(s string) *string {
	return &s
}
