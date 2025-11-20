-- +goose Up
-- Plagiarism reports table
CREATE TABLE execution.plagiarism_reports (
    id BIGSERIAL PRIMARY KEY,
    submission1_id BIGINT NOT NULL REFERENCES execution.submissions(id) ON DELETE CASCADE,
    submission2_id BIGINT NOT NULL REFERENCES execution.submissions(id) ON DELETE CASCADE,
    similarity_score DECIMAL(5,2) NOT NULL CHECK (similarity_score >= 0.0 AND similarity_score <= 1.0),
    algorithm VARCHAR(50) NOT NULL,
    is_reviewed BOOLEAN DEFAULT FALSE,
    reviewer_id BIGINT,
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'reviewed', 'dismissed', 'confirmed')),
    created_at TIMESTAMP DEFAULT NOW(),
    reviewed_at TIMESTAMP,
    review_notes TEXT,
    
    -- Ensure we don't have duplicate reports for the same pair
    UNIQUE (submission1_id, submission2_id),
    
    -- Prevent self-comparison
    CHECK (submission1_id != submission2_id)
);

-- Additional performance indexes
CREATE INDEX idx_plagiarism_reports_similarity ON execution.plagiarism_reports(similarity_score DESC);
CREATE INDEX idx_plagiarism_reports_status ON execution.plagiarism_reports(status, created_at DESC);
CREATE INDEX idx_plagiarism_reports_reviewer ON execution.plagiarism_reports(reviewer_id, reviewed_at DESC);
CREATE INDEX idx_plagiarism_reports_submission1 ON execution.plagiarism_reports(submission1_id);
CREATE INDEX idx_plagiarism_reports_submission2 ON execution.plagiarism_reports(submission2_id);

-- Add missing indexes for existing tables
CREATE INDEX CONCURRENTLY idx_submissions_judged_at ON execution.submissions(judged_at DESC) WHERE judged_at IS NOT NULL;
CREATE INDEX CONCURRENTLY idx_submissions_verdict_time ON execution.submissions(verdict, submitted_at DESC);
CREATE INDEX CONCURRENTLY idx_submission_test_results_verdict ON execution.submission_test_results(verdict, created_at DESC);
CREATE INDEX CONCURRENTLY idx_execution_logs_level_created ON execution.execution_logs(level, created_at DESC);
CREATE INDEX CONCURRENTLY idx_judge_workers_heartbeat ON execution.judge_workers(last_heartbeat DESC, status);

-- Add partial index for active submissions
CREATE INDEX CONCURRENTLY idx_submissions_active ON execution.submissions(id, problem_id, user_id) 
WHERE verdict IN ('pending', 'judging') OR judged_at IS NULL;

-- +goose Down
DROP INDEX IF EXISTS idx_submissions_active;
DROP INDEX IF EXISTS idx_execution_logs_level_created;
DROP INDEX IF EXISTS idx_submission_test_results_verdict;
DROP INDEX IF EXISTS idx_submissions_verdict_time;
DROP INDEX IF EXISTS idx_submissions_judged_at;
DROP INDEX IF EXISTS idx_plagiarism_reports_submission2;
DROP INDEX IF EXISTS idx_plagiarism_reports_submission1;
DROP INDEX IF EXISTS idx_plagiarism_reports_reviewer;
DROP INDEX IF EXISTS idx_plagiarism_reports_status;
DROP INDEX IF EXISTS idx_plagiarism_reports_similarity;
DROP TABLE IF EXISTS execution.plagiarism_reports;