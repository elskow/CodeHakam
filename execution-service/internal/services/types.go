package services

import "time"

// JudgeWorker represents a judge worker in the system
type JudgeWorker struct {
	ID                  int       `json:"id" db:"id"`
	WorkerName          string    `json:"worker_name" db:"worker_name"`
	Status              string    `json:"status" db:"status"`
	CurrentSubmissionID *int64    `json:"current_submission_id,omitempty" db:"current_submission_id"`
	StartedAt           time.Time `json:"started_at" db:"started_at"`
	LastHeartbeat       time.Time `json:"last_heartbeat" db:"last_heartbeat"`
	BoxID               *int      `json:"box_id,omitempty" db:"box_id"`
}

// Submission represents a code submission
type Submission struct {
	ID              int64      `json:"id" db:"id"`
	UserID          int64      `json:"user_id" db:"user_id"`
	ProblemID       int64      `json:"problem_id" db:"problem_id"`
	ContestID       *int64     `json:"contest_id,omitempty" db:"contest_id"`
	Language        string     `json:"language" db:"language"`
	CodeURL         string     `json:"code_url" db:"code_url"`
	Verdict         string     `json:"verdict" db:"verdict"`
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
