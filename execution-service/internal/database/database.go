package database

import (
	"context"
	"database/sql"
	"fmt"
	"time"

	"execution_service/internal/models"

	"github.com/jmoiron/sqlx"
	_ "github.com/lib/pq"
)

type DB struct {
	conn *sqlx.DB
}

func NewDB(databaseURL string, maxOpenConns, maxIdleConns int, connMaxLifetime time.Duration) (*DB, error) {
	conn, err := sqlx.Connect("postgres", databaseURL)
	if err != nil {
		return nil, fmt.Errorf("failed to connect to database: %w", err)
	}

	conn.SetMaxOpenConns(maxOpenConns)
	conn.SetMaxIdleConns(maxIdleConns)
	conn.SetConnMaxLifetime(connMaxLifetime)

	return &DB{conn: conn}, nil
}

func (db *DB) Close() error {
	return db.conn.Close()
}

func (db *DB) Ping(ctx context.Context) error {
	return db.conn.PingContext(ctx)
}

func (db *DB) CreateSubmission(ctx context.Context, submission *models.Submission) error {
	query := `
		INSERT INTO execution.submissions 
		(user_id, problem_id, contest_id, language, code_url, verdict, score, test_cases_passed, test_cases_total, is_public)
		VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
		RETURNING id, submitted_at`

	err := db.conn.QueryRowContext(ctx, query,
		submission.UserID,
		submission.ProblemID,
		submission.ContestID,
		submission.Language,
		submission.CodeURL,
		submission.Verdict,
		submission.Score,
		submission.TestCasesPassed,
		submission.TestCasesTotal,
		submission.IsPublic,
	).Scan(&submission.ID, &submission.SubmittedAt)

	if err != nil {
		return fmt.Errorf("failed to create submission: %w", err)
	}

	return nil
}

func (db *DB) GetSubmission(ctx context.Context, id int64) (*models.Submission, error) {
	query := `
		SELECT id, user_id, problem_id, contest_id, language, code_url, verdict, 
			   score, execution_time_ms, memory_used_kb, test_cases_passed, test_cases_total,
			   compile_output, is_public, submitted_at, judged_at
		FROM execution.submissions 
		WHERE id = $1`

	var submission models.Submission
	err := db.conn.GetContext(ctx, &submission, query, id)
	if err != nil {
		if err == sql.ErrNoRows {
			return nil, fmt.Errorf("submission not found")
		}
		return nil, fmt.Errorf("failed to get submission: %w", err)
	}

	return &submission, nil
}

func (db *DB) UpdateSubmissionResult(ctx context.Context, id int64, result *models.JudgeResult) error {
	query := `
		UPDATE execution.submissions 
		SET verdict = $2, execution_time_ms = $3, memory_used_kb = $4, 
			test_cases_passed = $5, test_cases_total = $6, judged_at = NOW()
		WHERE id = $1`

	_, err := db.conn.ExecContext(ctx, query,
		id,
		result.Verdict,
		result.ExecutionTimeMs,
		result.MemoryUsedKb,
		result.TestCasesPassed,
		result.TestCasesTotal,
	)

	if err != nil {
		return fmt.Errorf("failed to update submission result: %w", err)
	}

	return nil
}

func (db *DB) UpdateSubmissionCompilationError(ctx context.Context, id int64, compileOutput string) error {
	query := `
		UPDATE execution.submissions 
		SET verdict = 'CE', compile_output = $2, judged_at = NOW()
		WHERE id = $1`

	_, err := db.conn.ExecContext(ctx, query, id, compileOutput)
	if err != nil {
		return fmt.Errorf("failed to update compilation error: %w", err)
	}

	return nil
}

func (db *DB) CreateSubmissionTestResults(ctx context.Context, results []models.SubmissionTestResult) error {
	if len(results) == 0 {
		return nil
	}

	query := `
		INSERT INTO execution.submission_test_results 
		(submission_id, test_case_id, test_number, verdict, execution_time_ms, memory_used_kb, checker_output)
		VALUES ($1, $2, $3, $4, $5, $6, $7)`

	tx, err := db.conn.BeginTxx(ctx, nil)
	if err != nil {
		return fmt.Errorf("failed to begin transaction: %w", err)
	}
	defer tx.Rollback()

	for _, result := range results {
		_, err := tx.ExecContext(ctx, query,
			result.SubmissionID,
			result.TestCaseID,
			result.TestNumber,
			result.Verdict,
			result.ExecutionTimeMs,
			result.MemoryUsedKb,
			result.CheckerOutput,
		)
		if err != nil {
			return fmt.Errorf("failed to insert test result: %w", err)
		}
	}

	if err := tx.Commit(); err != nil {
		return fmt.Errorf("failed to commit transaction: %w", err)
	}

	return nil
}

func (db *DB) GetSupportedLanguages(ctx context.Context) ([]models.SupportedLanguage, error) {
	query := `
		SELECT id, language_code, language_name, version, compile_command, execute_command, is_enabled
		FROM execution.supported_languages
		WHERE is_enabled = true
		ORDER BY language_name`

	var languages []models.SupportedLanguage
	err := db.conn.SelectContext(ctx, &languages, query)
	if err != nil {
		return nil, fmt.Errorf("failed to get supported languages: %w", err)
	}

	return languages, nil
}

func (db *DB) GetLanguage(ctx context.Context, code string) (*models.SupportedLanguage, error) {
	query := `
		SELECT id, language_code, language_name, version, compile_command, execute_command, is_enabled
		FROM execution.supported_languages
		WHERE language_code = $1 AND is_enabled = true`

	var language models.SupportedLanguage
	err := db.conn.GetContext(ctx, &language, query, code)
	if err != nil {
		if err == sql.ErrNoRows {
			return nil, fmt.Errorf("language not found")
		}
		return nil, fmt.Errorf("failed to get language: %w", err)
	}

	return &language, nil
}

func (db *DB) CreateJudgeWorker(ctx context.Context, worker *models.JudgeWorker) error {
	query := `
		INSERT INTO execution.judge_workers (worker_name, status, box_id)
		VALUES ($1, $2, $3)
		RETURNING id, started_at, last_heartbeat`

	err := db.conn.QueryRowContext(ctx, query,
		worker.WorkerName,
		worker.Status,
		worker.BoxID,
	).Scan(&worker.ID, &worker.StartedAt, &worker.LastHeartbeat)

	if err != nil {
		return fmt.Errorf("failed to create judge worker: %w", err)
	}

	return nil
}

func (db *DB) UpdateWorkerStatus(ctx context.Context, workerID int, status string, submissionID *int64) error {
	query := `
		UPDATE execution.judge_workers 
		SET status = $2, current_submission_id = $3, last_heartbeat = NOW()
		WHERE id = $1`

	_, err := db.conn.ExecContext(ctx, query, workerID, status, submissionID)
	if err != nil {
		return fmt.Errorf("failed to update worker status: %w", err)
	}

	return nil
}

func (db *DB) CreateExecutionLog(ctx context.Context, log *models.ExecutionLog) error {
	query := `
		INSERT INTO execution.execution_logs (submission_id, level, message)
		VALUES ($1, $2, $3)
		RETURNING id, created_at`

	err := db.conn.QueryRowContext(ctx, query,
		log.SubmissionID,
		log.Level,
		log.Message,
	).Scan(&log.ID, &log.CreatedAt)

	if err != nil {
		return fmt.Errorf("failed to create execution log: %w", err)
	}

	return nil
}

func (db *DB) GetUserSubmissions(ctx context.Context, userID int64, limit, offset int) ([]models.Submission, error) {
	query := `
		SELECT id, user_id, problem_id, contest_id, language, code_url, verdict, 
			   score, execution_time_ms, memory_used_kb, test_cases_passed, test_cases_total,
			   compile_output, is_public, submitted_at, judged_at
		FROM execution.submissions 
		WHERE user_id = $1
		ORDER BY submitted_at DESC
		LIMIT $2 OFFSET $3`

	var submissions []models.Submission
	err := db.conn.SelectContext(ctx, &submissions, query, userID, limit, offset)
	if err != nil {
		return nil, fmt.Errorf("failed to get user submissions: %w", err)
	}

	return submissions, nil
}

func (db *DB) GetProblemSubmissions(ctx context.Context, problemID int64, limit, offset int) ([]models.Submission, error) {
	query := `
		SELECT id, user_id, problem_id, contest_id, language, code_url, verdict, 
			   score, execution_time_ms, memory_used_kb, test_cases_passed, test_cases_total,
			   compile_output, is_public, submitted_at, judged_at
		FROM execution.submissions 
		WHERE problem_id = $1
		ORDER BY submitted_at DESC
		LIMIT $2 OFFSET $3`

	var submissions []models.Submission
	err := db.conn.SelectContext(ctx, &submissions, query, problemID, limit, offset)
	if err != nil {
		return nil, fmt.Errorf("failed to get problem submissions: %w", err)
	}

	return submissions, nil
}
