package checker

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

	"execution_service/internal/models"
	"execution_service/internal/sandbox"
	"execution_service/internal/storage"
)

type CustomChecker struct {
	sandbox *sandbox.IsolateSandbox
	storage *storage.MinIOClient
	config  *CheckerConfig
}

type CheckerConfig struct {
	MaxCheckerSize     int64         `yaml:"max_checker_size"`
	MaxCheckerTime     time.Duration `yaml:"max_checker_time"`
	MaxCheckerMemory   int           `yaml:"max_checker_memory"`
	SupportedLanguages []string      `yaml:"supported_languages"`
	TempDir            string        `yaml:"temp_dir"`
}

type CheckerResult struct {
	IsCorrect     bool    `json:"is_correct"`
	Score         float64 `json:"score"`
	Message       string  `json:"message"`
	ExecutionTime int     `json:"execution_time_ms"`
	MemoryUsed    int     `json:"memory_used_kb"`
}

type CheckerCompilationResult struct {
	Success bool   `json:"success"`
	Error   string `json:"error"`
	Output  string `json:"output"`
}

func NewCustomChecker(sandbox *sandbox.IsolateSandbox, storage *storage.MinIOClient, config *CheckerConfig) *CustomChecker {
	return &CustomChecker{
		sandbox: sandbox,
		storage: storage,
		config:  config,
	}
}

func (cc *CustomChecker) ValidateOutput(ctx context.Context, testCase *models.TestCase, programOutput, expectedOutput string) (*CheckerResult, error) {
	// If no custom checker URL, fall back to exact matching
	if testCase.CheckerURL == "" {
		return cc.exactMatch(programOutput, expectedOutput), nil
	}

	// Download custom checker code
	checkerCode, err := cc.storage.DownloadCode(ctx, testCase.CheckerURL)
	if err != nil {
		return nil, fmt.Errorf("failed to download checker code: %w", err)
	}

	// Validate checker size
	if int64(len(checkerCode)) > cc.config.MaxCheckerSize {
		return &CheckerResult{
			IsCorrect: false,
			Score:     0.0,
			Message:   fmt.Sprintf("Checker code too large: %d bytes", len(checkerCode)),
		}, nil
	}

	// Determine checker language from file extension
	checkerLanguage := cc.detectCheckerLanguage(testCase.CheckerURL)
	if checkerLanguage == "" {
		return &CheckerResult{
			IsCorrect: false,
			Score:     0.0,
			Message:   "Unable to determine checker language",
		}, nil
	}

	// Compile checker
	compileResult, err := cc.compileChecker(ctx, checkerCode, checkerLanguage)
	if err != nil {
		return nil, fmt.Errorf("failed to compile checker: %w", err)
	}

	if !compileResult.Success {
		return &CheckerResult{
			IsCorrect: false,
			Score:     0.0,
			Message:   fmt.Sprintf("Checker compilation failed: %s", compileResult.Error),
		}, nil
	}

	// Execute checker
	result, err := cc.executeChecker(ctx, programOutput, expectedOutput, checkerLanguage)
	if err != nil {
		return nil, fmt.Errorf("failed to execute checker: %w", err)
	}

	return result, nil
}

func (cc *CustomChecker) compileChecker(ctx context.Context, checkerCode []byte, language string) (*CheckerCompilationResult, error) {
	boxID, err := cc.sandbox.CreateBox()
	if err != nil {
		return nil, fmt.Errorf("failed to create isolate box: %w", err)
	}
	defer cc.sandbox.CleanupBox(boxID)

	boxDir := cc.sandbox.GetBoxDir(boxID)
	checkerFile := filepath.Join(boxDir, "checker"+cc.getFileExtension(language))

	err = os.WriteFile(checkerFile, checkerCode, 0644)
	if err != nil {
		return nil, fmt.Errorf("failed to write checker file: %w", err)
	}

	// Get language-specific compile command
	compileCmd := cc.getCompileCommand(language, "checker", "checker")
	if compileCmd == "" {
		// No compilation needed for interpreted languages
		return &CheckerCompilationResult{Success: true}, nil
	}

	// Execute compilation in sandbox
	args := []string{
		"--box-id=" + strconv.Itoa(boxID),
		"--cg",
		"--cg-timing",
		"--processes=5",
		"--mem=262144", // 256MB for compilation
		"--time=" + strconv.Itoa(int(cc.config.MaxCheckerTime.Seconds())),
		"--wall-time=" + strconv.Itoa(int(cc.config.MaxCheckerTime.Seconds()*2)),
		"--fsize=16384", // 16MB max file size
		"--env=PATH=/usr/bin:/bin",
		"--dir=/etc:noexec",
		"--dir=/usr:noexec",
		"--dir=/lib:noexec",
		"--dir=/lib64:noexec",
		"--dir=/tmp:rw",
		"--run",
		"--",
		"/bin/bash",
		"-c",
		compileCmd,
	}

	cmd := exec.CommandContext(ctx, cc.sandbox.GetPath(), args...)
	cmd.Dir = boxDir

	var stdout, stderr bytes.Buffer
	cmd.Stdout = &stdout
	cmd.Stderr = &stderr

	err = cmd.Run()
	if err != nil {
		return &CheckerCompilationResult{
			Success: false,
			Output:  stdout.String(),
			Error:   stderr.String(),
		}, nil
	}

	return &CheckerCompilationResult{
		Success: true,
		Output:  stdout.String(),
		Error:   stderr.String(),
	}, nil
}

func (cc *CustomChecker) executeChecker(ctx context.Context, programOutput, expectedOutput, language string) (*CheckerResult, error) {
	boxID, err := cc.sandbox.CreateBox()
	if err != nil {
		return nil, fmt.Errorf("failed to create isolate box: %w", err)
	}
	defer cc.sandbox.CleanupBox(boxID)

	boxDir := cc.sandbox.GetBoxDir(boxID)

	// Write input files for checker
	inputFile := filepath.Join(boxDir, "input.txt")
	outputFile := filepath.Join(boxDir, "output.txt")
	expectedFile := filepath.Join(boxDir, "expected.txt")

	if err := os.WriteFile(inputFile, []byte(programOutput), 0644); err != nil {
		return nil, fmt.Errorf("failed to write input file: %w", err)
	}

	if err := os.WriteFile(outputFile, []byte(programOutput), 0644); err != nil {
		return nil, fmt.Errorf("failed to write output file: %w", err)
	}

	if err := os.WriteFile(expectedFile, []byte(expectedOutput), 0644); err != nil {
		return nil, fmt.Errorf("failed to write expected file: %w", err)
	}

	// Get language-specific execute command
	executeCmd := cc.getExecuteCommand(language, "checker", "input.txt", "output.txt", "expected.txt")
	if executeCmd == "" {
		return &CheckerResult{
			IsCorrect: false,
			Score:     0.0,
			Message:   "Unsupported checker language",
		}, nil
	}

	// Execute checker in sandbox
	args := []string{
		"--box-id=" + strconv.Itoa(boxID),
		"--cg",
		"--cg-timing",
		"--processes=1",
		"--mem=" + strconv.Itoa(cc.config.MaxCheckerMemory),
		"--time=" + strconv.Itoa(int(cc.config.MaxCheckerTime.Seconds())),
		"--wall-time=" + strconv.Itoa(int(cc.config.MaxCheckerTime.Seconds()*2)),
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
		"--stdout=checker_output.txt",
		"--stderr=error.txt",
		"--meta=meta.txt",
		"--run",
		"--",
		"/bin/bash",
		"-c",
		executeCmd,
	}

	cmd := exec.CommandContext(ctx, cc.sandbox.GetPath(), args...)
	cmd.Dir = boxDir

	startTime := time.Now()
	err = cmd.Run()
	executionTime := time.Since(startTime)

	if err != nil {
		// Try to read any output even if execution failed
		outputFile = filepath.Join(boxDir, "checker_output.txt")
		errorFile := filepath.Join(boxDir, "error.txt")

		var output, errorStr []byte
		if output, _ = os.ReadFile(outputFile); len(output) > 0 {
			return cc.parseCheckerOutput(string(output), executionTime, 0), nil
		}

		if errorStr, _ = os.ReadFile(errorFile); len(errorStr) > 0 {
			return &CheckerResult{
				IsCorrect: false,
				Score:     0.0,
				Message:   fmt.Sprintf("Checker execution failed: %s", string(errorStr)),
			}, nil
		}

		return &CheckerResult{
			IsCorrect: false,
			Score:     0.0,
			Message:   fmt.Sprintf("Checker execution failed: %v", err),
		}, nil
	}

	// Read checker output
	outputFile = filepath.Join(boxDir, "checker_output.txt")
	output, err := os.ReadFile(outputFile)
	if err != nil {
		return nil, fmt.Errorf("failed to read checker output: %w", err)
	}

	// Parse execution metrics from meta.txt
	metaFile := filepath.Join(boxDir, "meta.txt")
	meta, _ := os.ReadFile(metaFile)
	_, memoryKb := cc.parseMetaFile(string(meta))

	return cc.parseCheckerOutput(string(output), executionTime, memoryKb), nil
}

func (cc *CustomChecker) parseCheckerOutput(output string, executionTime time.Duration, memoryKb int) *CheckerResult {
	// Parse checker output format
	// Expected format: "score message" or "CORRECT/INCORRECT message"
	lines := strings.Split(strings.TrimSpace(output), "\n")
	if len(lines) == 0 {
		return &CheckerResult{
			IsCorrect: false,
			Score:     0.0,
			Message:   "No output from checker",
		}
	}

	firstLine := strings.TrimSpace(lines[0])

	// Check for simple CORRECT/INCORRECT format
	if strings.ToUpper(firstLine) == "CORRECT" {
		message := "Correct answer"
		if len(lines) > 1 {
			message = strings.TrimSpace(lines[1])
		}
		return &CheckerResult{
			IsCorrect:     true,
			Score:         1.0,
			Message:       message,
			ExecutionTime: int(executionTime.Milliseconds()),
			MemoryUsed:    memoryKb,
		}
	}

	if strings.ToUpper(firstLine) == "INCORRECT" {
		message := "Incorrect answer"
		if len(lines) > 1 {
			message = strings.TrimSpace(lines[1])
		}
		return &CheckerResult{
			IsCorrect:     false,
			Score:         0.0,
			Message:       message,
			ExecutionTime: int(executionTime.Milliseconds()),
			MemoryUsed:    memoryKb,
		}
	}

	// Try to parse as "score message"
	parts := strings.Fields(firstLine)
	if len(parts) >= 1 {
		if score, err := strconv.ParseFloat(parts[0], 64); err == nil {
			message := strings.Join(parts[1:], " ")
			if message == "" {
				message = "Checker completed"
			}

			// Normalize score to 0-1 range
			normalizedScore := score
			if normalizedScore > 1.0 {
				normalizedScore = normalizedScore / 100.0
			}

			return &CheckerResult{
				IsCorrect:     normalizedScore > 0.5,
				Score:         normalizedScore,
				Message:       message,
				ExecutionTime: int(executionTime.Milliseconds()),
				MemoryUsed:    memoryKb,
			}
		}
	}

	// Default case - treat as incorrect
	return &CheckerResult{
		IsCorrect:     false,
		Score:         0.0,
		Message:       firstLine,
		ExecutionTime: int(executionTime.Milliseconds()),
		MemoryUsed:    memoryKb,
	}
}

func (cc *CustomChecker) exactMatch(programOutput, expectedOutput string) *CheckerResult {
	program := strings.TrimSpace(programOutput)
	expected := strings.TrimSpace(expectedOutput)

	if program == expected {
		return &CheckerResult{
			IsCorrect: true,
			Score:     1.0,
			Message:   "Correct answer",
		}
	}

	return &CheckerResult{
		IsCorrect: false,
		Score:     0.0,
		Message:   "Wrong answer",
	}
}

func (cc *CustomChecker) detectCheckerLanguage(checkerURL string) string {
	if strings.HasSuffix(strings.ToLower(checkerURL), ".cpp") {
		return "cpp"
	}
	if strings.HasSuffix(strings.ToLower(checkerURL), ".c") {
		return "c"
	}
	if strings.HasSuffix(strings.ToLower(checkerURL), ".java") {
		return "java"
	}
	if strings.HasSuffix(strings.ToLower(checkerURL), ".py") {
		return "python"
	}
	if strings.HasSuffix(strings.ToLower(checkerURL), ".go") {
		return "go"
	}
	if strings.HasSuffix(strings.ToLower(checkerURL), ".js") {
		return "javascript"
	}
	if strings.HasSuffix(strings.ToLower(checkerURL), ".sh") {
		return "bash"
	}

	return ""
}

func (cc *CustomChecker) getFileExtension(language string) string {
	extensions := map[string]string{
		"cpp":        ".cpp",
		"c":          ".c",
		"java":       ".java",
		"python":     ".py",
		"go":         ".go",
		"javascript": ".js",
		"bash":       ".sh",
	}

	if ext, exists := extensions[language]; exists {
		return ext
	}
	return ".txt"
}

func (cc *CustomChecker) getCompileCommand(language, inputFile, outputFile string) string {
	commands := map[string]string{
		"cpp":  fmt.Sprintf("g++ -O2 -std=c++17 -o %s %s", outputFile, inputFile),
		"c":    fmt.Sprintf("gcc -O2 -std=c11 -o %s %s", outputFile, inputFile),
		"java": fmt.Sprintf("javac %s.java", strings.TrimSuffix(inputFile, ".java")),
		"go":   fmt.Sprintf("go build -o %s %s", outputFile, inputFile),
	}

	if cmd, exists := commands[language]; exists {
		return cmd
	}
	return ""
}

func (cc *CustomChecker) getExecuteCommand(language, checkerFile, inputFile, outputFile, expectedFile string) string {
	commands := map[string]string{
		"cpp":        fmt.Sprintf("./%s %s %s %s", checkerFile, inputFile, outputFile, expectedFile),
		"c":          fmt.Sprintf("./%s %s %s %s", checkerFile, inputFile, outputFile, expectedFile),
		"java":       fmt.Sprintf("java %s %s %s %s", strings.TrimSuffix(checkerFile, ".class"), inputFile, outputFile, expectedFile),
		"python":     fmt.Sprintf("python3 %s %s %s %s", checkerFile, inputFile, outputFile, expectedFile),
		"go":         fmt.Sprintf("./%s %s %s %s", checkerFile, inputFile, outputFile, expectedFile),
		"javascript": fmt.Sprintf("node %s %s %s %s", checkerFile, inputFile, outputFile, expectedFile),
		"bash":       fmt.Sprintf("bash %s %s %s %s", checkerFile, inputFile, outputFile, expectedFile),
	}

	if cmd, exists := commands[language]; exists {
		return cmd
	}
	return ""
}

func (cc *CustomChecker) parseMetaFile(meta string) (timeMs, memoryKb int) {
	lines := strings.Split(meta, "\n")
	for _, line := range lines {
		line = strings.TrimSpace(line)
		if strings.HasPrefix(line, "time:") {
			timeStr := strings.TrimSpace(strings.TrimPrefix(line, "time:"))
			if time, err := strconv.ParseFloat(timeStr, 64); err == nil {
				timeMs = int(time * 1000)
			}
		}
		if strings.HasPrefix(line, "max-rss:") {
			memStr := strings.TrimSpace(strings.TrimPrefix(line, "max-rss:"))
			if mem, err := strconv.Atoi(memStr); err == nil {
				memoryKb = mem
			}
		}
	}
	return
}

func (cc *CustomChecker) GetDefaultConfig() *CheckerConfig {
	return &CheckerConfig{
		MaxCheckerSize:     65536, // 64KB
		MaxCheckerTime:     10 * time.Second,
		MaxCheckerMemory:   131072, // 128MB
		SupportedLanguages: []string{"cpp", "c", "java", "python", "go", "javascript", "bash"},
		TempDir:            "/tmp/checker",
	}
}
