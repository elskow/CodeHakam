package models

import (
	"database/sql/driver"
	"time"
)

type Verdict string

const (
	VerdictPending  Verdict = "pending"
	VerdictAccepted Verdict = "AC"
	VerdictWrongAns Verdict = "WA"
	VerdictTimeLim  Verdict = "TLE"
	VerdictMemLim   Verdict = "MLE"
	VerdictRuntime  Verdict = "RE"
	VerdictCompile  Verdict = "CE"
	VerdictInternal Verdict = "IE"
)

type Submission struct {
	ID              int64      `json:"id" db:"id"`
	UserID          int64      `json:"user_id" db:"user_id"`
	ProblemID       int64      `json:"problem_id" db:"problem_id"`
	ContestID       *int64     `json:"contest_id,omitempty" db:"contest_id"`
	Language        string     `json:"language" db:"language"`
	CodeURL         string     `json:"code_url" db:"code_url"`
	Verdict         Verdict    `json:"verdict" db:"verdict"`
	Score           int        `json:"score" db:"score"`
	ExecutionTimeMs *int       `json:"execution_time_ms,omitempty" db:"execution_time_ms"`
	MemoryUsedKb    *int       `json:"memory_used_kb,omitempty" db:"memory_used_kb"`
	TestCasesPassed int        `json:"test_cases_passed" db:"test_cases_passed"`
	TestCasesTotal  *int       `json:"test_cases_total,omitempty" db:"test_cases_total"`
	CompileOutput   *string    `json:"compile_output,omitempty" db:"compile_output"`
	IsPublic        bool       `json:"is_public" db:"is_public"`
	SubmittedAt     time.Time  `json:"submitted_at" db:"submitted_at"`
	JudgedAt        *time.Time `json:"judged_at,omitempty" db:"judged_at"`
}

type SubmissionTestResult struct {
	ID              int64     `json:"id" db:"id"`
	SubmissionID    int64     `json:"submission_id" db:"submission_id"`
	TestCaseID      int64     `json:"test_case_id" db:"test_case_id"`
	TestNumber      int       `json:"test_number" db:"test_number"`
	Verdict         Verdict   `json:"verdict" db:"verdict"`
	ExecutionTimeMs *int      `json:"execution_time_ms,omitempty" db:"execution_time_ms"`
	MemoryUsedKb    *int      `json:"memory_used_kb,omitempty" db:"memory_used_kb"`
	CheckerOutput   *string   `json:"checker_output,omitempty" db:"checker_output"`
	CreatedAt       time.Time `json:"created_at" db:"created_at"`
}

type SupportedLanguage struct {
	ID             int     `json:"id" db:"id"`
	LanguageCode   string  `json:"language_code" db:"language_code"`
	LanguageName   string  `json:"language_name" db:"language_name"`
	Version        string  `json:"version,omitempty" db:"version"`
	CompileCommand *string `json:"compile_command,omitempty" db:"compile_command"`
	ExecuteCommand string  `json:"execute_command" db:"execute_command"`
	IsEnabled      bool    `json:"is_enabled" db:"is_enabled"`
}

type JudgeWorker struct {
	ID                  int       `json:"id" db:"id"`
	WorkerName          string    `json:"worker_name" db:"worker_name"`
	Status              string    `json:"status" db:"status"`
	CurrentSubmissionID *int64    `json:"current_submission_id,omitempty" db:"current_submission_id"`
	StartedAt           time.Time `json:"started_at" db:"started_at"`
	LastHeartbeat       time.Time `json:"last_heartbeat" db:"last_heartbeat"`
	BoxID               *int      `json:"box_id,omitempty" db:"box_id"`
}

type ExecutionLog struct {
	ID           int64     `json:"id" db:"id"`
	SubmissionID int64     `json:"submission_id" db:"submission_id"`
	Level        string    `json:"level" db:"level"`
	Message      string    `json:"message" db:"message"`
	CreatedAt    time.Time `json:"created_at" db:"created_at"`
}

type JudgeRequest struct {
	SubmissionID  int64  `json:"submission_id"`
	UserID        int64  `json:"user_id"`
	ProblemID     int64  `json:"problem_id"`
	Language      string `json:"language"`
	CodeURL       string `json:"code_url"`
	TimeLimitMs   int    `json:"time_limit_ms"`
	MemoryLimitKb int    `json:"memory_limit_kb"`
	Priority      int    `json:"priority"`
}

type JudgeResult struct {
	SubmissionID    int64   `json:"submission_id"`
	Verdict         Verdict `json:"verdict"`
	ExecutionTimeMs int     `json:"execution_time_ms"`
	MemoryUsedKb    int     `json:"memory_used_kb"`
	TestCasesPassed int     `json:"test_cases_passed"`
	TestCasesTotal  int     `json:"test_cases_total"`
}

type TestCase struct {
	ID          int64  `json:"id"`
	InputURL    string `json:"input_url"`
	OutputURL   string `json:"output_url"`
	IsSample    bool   `json:"is_sample"`
	TimeLimit   int    `json:"time_limit"`
	MemoryLimit int    `json:"memory_limit"`
}

func (v Verdict) Value() (driver.Value, error) {
	return string(v), nil
}

func (v *Verdict) Scan(value interface{}) error {
	if value == nil {
		*v = VerdictPending
		return nil
	}
	if str, ok := value.(string); ok {
		*v = Verdict(str)
		return nil
	}
	return nil
}

type EventMessage struct {
	EventType string                 `json:"event_type"`
	Data      map[string]interface{} `json:"data"`
	Timestamp time.Time              `json:"timestamp"`
}
