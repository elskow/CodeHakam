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
	"execution_service/internal/services"
)

type SandboxService struct {
	isolateSandbox    *IsolateSandbox
	languageService   *services.LanguageService
	securityValidator *SecurityValidator
}

func NewSandboxService(cfg *config.IsolateConfig, languageService *services.LanguageService) *SandboxService {
	securityConfig := &SecurityConfig{}
	validator := NewSecurityValidator(securityConfig)

	return &SandboxService{
		isolateSandbox:    NewIsolateSandbox(cfg),
		languageService:   languageService,
		securityValidator: validator,
	}
}

func (ss *SandboxService) Compile(ctx context.Context, language string, code []byte, timeLimit time.Duration) (*CompileResult, error) {
	boxID, err := ss.isolateSandbox.CreateBox()
	if err != nil {
		return nil, fmt.Errorf("failed to create isolate box: %w", err)
	}
	defer ss.isolateSandbox.CleanupBox(boxID)

	boxDir := ss.isolateSandbox.GetBoxDir(boxID)
	codeFile := filepath.Join(boxDir, "code"+getFileExtension(language))

	err = os.WriteFile(codeFile, code, 0644)
	if err != nil {
		return nil, fmt.Errorf("failed to write code file: %w", err)
	}

	// Get language configuration from service
	langConfig, err := ss.languageService.GetLanguageConfig(language)
	if err != nil {
		return nil, fmt.Errorf("failed to get language config: %w", err)
	}

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

	cmd := exec.CommandContext(ctx, ss.isolateSandbox.GetPath(), args...)
	cmd.Dir = boxDir

	err = cmd.Run()
	if err != nil {
		return ss.isolateSandbox.parseCompilationResult(boxID, err, timeLimit, memoryLimit)
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

func (ss *SandboxService) Execute(ctx context.Context, language string, input []byte, timeLimit time.Duration, memoryLimit int) (*ExecutionResult, error) {
	boxID, err := ss.isolateSandbox.CreateBox()
	if err != nil {
		return nil, fmt.Errorf("failed to create isolate box: %w", err)
	}
	defer ss.isolateSandbox.CleanupBox(boxID)

	boxDir := ss.isolateSandbox.GetBoxDir(boxID)
	inputFile := filepath.Join(boxDir, "input.txt")

	err = os.WriteFile(inputFile, input, 0644)
	if err != nil {
		return nil, fmt.Errorf("failed to write input file: %w", err)
	}

	// Get language configuration from service
	langConfig, err := ss.languageService.GetLanguageConfig(language)
	if err != nil {
		return nil, fmt.Errorf("failed to get language config: %w", err)
	}

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

	cmd := exec.CommandContext(ctx, ss.isolateSandbox.GetPath(), args...)
	cmd.Dir = boxDir

	err = cmd.Run()
	if err != nil {
		return ss.isolateSandbox.parseExecutionResult(boxID, 1, timeLimit, memoryLimit)
	}

	return ss.isolateSandbox.parseExecutionResult(boxID, 0, timeLimit, memoryLimit)
}

func (ss *SandboxService) GetSandbox() *IsolateSandbox {
	return ss.isolateSandbox
}
