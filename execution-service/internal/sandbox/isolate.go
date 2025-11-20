package sandbox

import (
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
	config            *config.IsolateConfig
	securityValidator *SecurityValidator
}

type ExecutionResult struct {
	Verdict       models.Verdict
	Output        string
	Error         string
	ExecutionTime int
	MemoryUsed    int
	ExitCode      int
	WallTime      int
	Signals       string
}

type CompileResult struct {
	Success bool
	Output  string
	Error   string
}

func NewIsolateSandbox(cfg *config.IsolateConfig) *IsolateSandbox {
	securityConfig := &SecurityConfig{}
	validator := NewSecurityValidator(securityConfig)

	return &IsolateSandbox{
		config:            cfg,
		securityValidator: validator,
	}
}

func (i *IsolateSandbox) Compile(ctx context.Context, language string, code []byte, timeLimit time.Duration) (*CompileResult, error) {
	boxID, err := i.CreateBox()
	if err != nil {
		return nil, fmt.Errorf("failed to create isolate box: %w", err)
	}
	defer i.CleanupBox(boxID)

	boxDir := i.GetBoxDir(boxID)
	codeFile := filepath.Join(boxDir, "code"+getFileExtension(language))

	err = os.WriteFile(codeFile, code, 0644)
	if err != nil {
		return nil, fmt.Errorf("failed to write code file: %w", err)
	}

	langConfig := getLanguageConfig(language)

	// If no compilation required, return success
	if langConfig.CompileCommand == nil {
		return &CompileResult{
			Success: true,
			Output:  "No compilation required",
			Error:   "",
		}, nil
	}

	compileCmd := strings.ReplaceAll(*langConfig.CompileCommand, "{executable}", "program")
	compileCmd = strings.ReplaceAll(compileCmd, "{input}", "code"+getFileExtension(language))
	compileCmd = strings.ReplaceAll(compileCmd, "{classname}", "Main")

	// Convert time limit to seconds for isolate, ensure minimum 1 second
	timeSec := int(timeLimit.Seconds())
	if timeSec < 1 {
		timeSec = 1
	}
	wallTimeSec := timeSec * 2
	memoryLimit := 524288 // 512MB default for compilation

	args := []string{
		"--box-id=" + strconv.Itoa(boxID),
		"--cg",
		"--cg-timing",
		"--processes=1",
		"--mem=" + strconv.Itoa(memoryLimit),
		"--time=" + strconv.Itoa(timeSec),
		"--wall-time=" + strconv.Itoa(wallTimeSec),
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
		"--stdout=output.txt",
		"--stderr=error.txt",
		"--meta=meta.txt",
		"--run",
		"--",
		"/bin/bash",
		"-c",
		compileCmd,
	}

	cmd := exec.CommandContext(ctx, i.config.Path, args...)
	cmd.Dir = boxDir

	err = cmd.Run()
	if err != nil {
		return i.parseCompilationResult(boxID, err, timeLimit, memoryLimit)
	}

	// Read compilation output
	outputFile := filepath.Join(boxDir, "output.txt")
	errorFile := filepath.Join(boxDir, "error.txt")

	output, _ := os.ReadFile(outputFile)
	errorMsg, _ := os.ReadFile(errorFile)

	return &CompileResult{
		Success: true,
		Output:  string(output),
		Error:   string(errorMsg),
	}, nil
}

func (i *IsolateSandbox) Execute(ctx context.Context, language string, input []byte, timeLimit time.Duration, memoryLimit int) (*ExecutionResult, error) {
	boxID, err := i.CreateBox()
	if err != nil {
		return nil, fmt.Errorf("failed to create isolate box: %w", err)
	}
	defer i.CleanupBox(boxID)

	boxDir := i.GetBoxDir(boxID)
	inputFile := filepath.Join(boxDir, "input.txt")

	err = os.WriteFile(inputFile, input, 0644)
	if err != nil {
		return nil, fmt.Errorf("failed to write input file: %w", err)
	}

	langConfig := getLanguageConfig(language)
	runCmd := strings.ReplaceAll(langConfig.ExecuteCommand, "{executable}", "program")
	runCmd = strings.ReplaceAll(runCmd, "{input}", "input.txt")
	runCmd = strings.ReplaceAll(runCmd, "{classname}", "Main")

	// Convert time limit to seconds for isolate, ensure minimum 1 second
	timeSec := int(timeLimit.Seconds())
	if timeSec < 1 {
		timeSec = 1
	}
	wallTimeSec := timeSec * 2

	args := []string{
		"--box-id=" + strconv.Itoa(boxID),
		"--cg",
		"--cg-timing",
		"--processes=1",
		"--mem=" + strconv.Itoa(memoryLimit),
		"--time=" + strconv.Itoa(timeSec),
		"--wall-time=" + strconv.Itoa(wallTimeSec),
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
		"--meta=meta.txt",
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
		return i.parseExecutionResult(boxID, 1, timeLimit, memoryLimit)
	}

	return i.parseExecutionResult(boxID, 0, timeLimit, memoryLimit)
}

func (i *IsolateSandbox) parseExecutionResult(boxID int, exitCode int, timeLimit time.Duration, memoryLimit int) (*ExecutionResult, error) {
	boxDir := i.GetBoxDir(boxID)

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

	result.ExecutionTime, result.MemoryUsed, result.WallTime, result.Signals = i.parseMetaFile(string(meta))

	result.Verdict = i.determineVerdict(exitCode, result.ExecutionTime, result.MemoryUsed, result.WallTime, timeLimit, memoryLimit)

	// Validate resource usage for security anomalies
	resourceViolations := i.securityValidator.ValidateResourceUsage(
		result.ExecutionTime, result.WallTime, result.MemoryUsed, timeLimit, memoryLimit)

	// Log security violations but don't fail execution for non-critical issues
	for _, violation := range resourceViolations {
		if violation.Severity == "critical" {
			result.Verdict = models.VerdictRuntime
			result.Error = fmt.Sprintf("Security violation: %s", violation.Description)
			break
		}
	}

	return result, nil
}

func (i *IsolateSandbox) parseCompilationResult(boxID int, err error, timeLimit time.Duration, memoryLimit int) (*CompileResult, error) {
	boxDir := i.GetBoxDir(boxID)

	outputFile := filepath.Join(boxDir, "output.txt")
	errorFile := filepath.Join(boxDir, "error.txt")

	output, _ := os.ReadFile(outputFile)
	errorStr, _ := os.ReadFile(errorFile)

	// Check if it was a timeout
	if exitErr, ok := err.(*exec.ExitError); ok {
		if exitErr.ExitCode() == 124 {
			return &CompileResult{
				Success: false,
				Output:  string(output),
				Error:   "Compilation timeout (limit: " + strconv.Itoa(int(timeLimit.Seconds())) + "s)",
			}, nil
		}
	}

	return &CompileResult{
		Success: false,
		Output:  string(output),
		Error:   string(errorStr),
	}, nil
}

func (i *IsolateSandbox) parseMetaFile(meta string) (timeMs, memoryKb, wallTimeMs int, signals string) {
	lines := strings.Split(meta, "\n")
	for _, line := range lines {
		line = strings.TrimSpace(line)
		if strings.HasPrefix(line, "time:") {
			timeStr := strings.TrimSpace(strings.TrimPrefix(line, "time:"))
			if time, err := strconv.ParseFloat(timeStr, 64); err == nil {
				// Convert seconds to milliseconds
				timeMs = int(time * 1000)
			}
		}
		if strings.HasPrefix(line, "time-wall:") {
			timeStr := strings.TrimSpace(strings.TrimPrefix(line, "time-wall:"))
			if time, err := strconv.ParseFloat(timeStr, 64); err == nil {
				// Convert seconds to milliseconds
				wallTimeMs = int(time * 1000)
			}
		}
		if strings.HasPrefix(line, "max-rss:") {
			memStr := strings.TrimSpace(strings.TrimPrefix(line, "max-rss:"))
			if mem, err := strconv.Atoi(memStr); err == nil {
				// max-rss is already in KB
				memoryKb = mem
			}
		}
		if strings.HasPrefix(line, "mem:") {
			memStr := strings.TrimSpace(strings.TrimPrefix(line, "mem:"))
			if mem, err := strconv.Atoi(memStr); err == nil {
				// mem is in bytes, convert to KB
				memKB := mem / 1024
				if memKB > memoryKb {
					memoryKb = memKB
				}
			}
		}
		if strings.HasPrefix(line, "signals:") {
			signals = strings.TrimSpace(strings.TrimPrefix(line, "signals:"))
		}
	}
	return
}

func (i *IsolateSandbox) determineVerdict(exitCode, timeMs, memoryKb, wallTimeMs int, timeLimit time.Duration, memoryLimit int) models.Verdict {
	timeLimitMs := int(timeLimit.Milliseconds())

	// Use wall-time for time limit checking (more accurate for user programs)
	effectiveTime := wallTimeMs
	if wallTimeMs == 0 {
		effectiveTime = timeMs
	}

	// Check time limit exceeded
	if effectiveTime > timeLimitMs {
		return models.VerdictTimeLim
	}

	// Check memory limit exceeded
	if memoryKb > memoryLimit {
		return models.VerdictMemLim
	}

	// Check runtime errors
	if exitCode != 0 {
		// Check for specific exit codes from Isolate
		switch exitCode {
		case 124: // timeout (from timeout command)
			return models.VerdictTimeLim
		case 125: // timeout command failed
			return models.VerdictRuntime
		case 126: // command not executable
			return models.VerdictRuntime
		case 127: // command not found
			return models.VerdictRuntime
		case 130: // SIGINT (Ctrl+C)
			return models.VerdictRuntime
		case 134: // SIGABRT
			return models.VerdictRuntime
		case 137: // SIGKILL (memory limit)
			return models.VerdictMemLim
		case 139: // SIGSEGV
			return models.VerdictRuntime
		case 143: // SIGTERM
			return models.VerdictRuntime
		case 255: // Isolate sandbox violation
			return models.VerdictRuntime
		default:
			return models.VerdictRuntime
		}
	}

	return models.VerdictAccepted
}

func (i *IsolateSandbox) CreateBox() (int, error) {
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

func (i *IsolateSandbox) CleanupBox(boxID int) {
	cmd := exec.Command(i.config.Path, "--box-id="+strconv.Itoa(boxID), "--cleanup")
	cmd.Run()
}

func (i *IsolateSandbox) GetBoxDir(boxID int) string {
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

func (i *IsolateSandbox) GetPath() string {
	return i.config.Path
}
