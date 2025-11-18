-- +goose Up
CREATE SCHEMA IF NOT EXISTS execution;

-- Submissions table
CREATE TABLE execution.submissions (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    problem_id BIGINT NOT NULL,
    contest_id BIGINT,
    language VARCHAR(20) NOT NULL,
    code_url TEXT NOT NULL,
    verdict VARCHAR(20) DEFAULT 'pending',
    score INTEGER DEFAULT 0,
    execution_time_ms INTEGER,
    memory_used_kb INTEGER,
    test_cases_passed INTEGER DEFAULT 0,
    test_cases_total INTEGER,
    compile_output TEXT,
    is_public BOOLEAN DEFAULT FALSE,
    submitted_at TIMESTAMP DEFAULT NOW(),
    judged_at TIMESTAMP
);

-- Submission test results
CREATE TABLE execution.submission_test_results (
    id BIGSERIAL PRIMARY KEY,
    submission_id BIGINT REFERENCES execution.submissions(id) ON DELETE CASCADE,
    test_case_id BIGINT NOT NULL,
    test_number INTEGER NOT NULL,
    verdict VARCHAR(20) NOT NULL,
    execution_time_ms INTEGER,
    memory_used_kb INTEGER,
    checker_output TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Supported languages
CREATE TABLE execution.supported_languages (
    id SERIAL PRIMARY KEY,
    language_code VARCHAR(20) UNIQUE NOT NULL,
    language_name VARCHAR(50) NOT NULL,
    version VARCHAR(20),
    compile_command TEXT,
    execute_command TEXT,
    is_enabled BOOLEAN DEFAULT TRUE
);

-- Judge workers
CREATE TABLE execution.judge_workers (
    id SERIAL PRIMARY KEY,
    worker_name VARCHAR(100) UNIQUE NOT NULL,
    status VARCHAR(20) DEFAULT 'idle',
    current_submission_id BIGINT REFERENCES execution.submissions(id),
    started_at TIMESTAMP DEFAULT NOW(),
    last_heartbeat TIMESTAMP DEFAULT NOW(),
    box_id INTEGER
);

-- Execution logs
CREATE TABLE execution.execution_logs (
    id BIGSERIAL PRIMARY KEY,
    submission_id BIGINT REFERENCES execution.submissions(id) ON DELETE CASCADE,
    level VARCHAR(10) NOT NULL,
    message TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_submissions_user ON execution.submissions(user_id, submitted_at DESC);
CREATE INDEX idx_submissions_problem ON execution.submissions(problem_id);
CREATE INDEX idx_submissions_contest ON execution.submissions(contest_id);
CREATE INDEX idx_submissions_verdict ON execution.submissions(verdict);
CREATE INDEX idx_submissions_submitted_at ON execution.submissions(submitted_at DESC);
CREATE INDEX idx_execution_results_submission ON execution.submission_test_results(submission_id);
CREATE INDEX idx_judge_workers_status ON execution.judge_workers(status);
CREATE INDEX idx_execution_logs_submission ON execution.execution_logs(submission_id);

-- Insert supported languages
INSERT INTO execution.supported_languages (language_code, language_name, version, compile_command, execute_command) VALUES
('cpp', 'C++17', '17', 'g++ -O2 -std=c++17 -o {output} {input}', './{executable}'),
('java', 'Java 17', '17', 'javac {input}', 'java {classname}'),
('python', 'Python 3', '3.10', NULL, 'python3 {input}'),
('go', 'Go 1.21', '1.21', 'go build -o {output} {input}', './{executable}'),
('c', 'C11', '11', 'gcc -O2 -std=c11 -o {output} {input}', './{executable}');

-- +goose Down
DROP TABLE IF EXISTS execution.execution_logs;
DROP TABLE IF EXISTS execution.judge_workers;
DROP TABLE IF EXISTS execution.supported_languages;
DROP TABLE IF EXISTS execution.submission_test_results;
DROP TABLE IF EXISTS execution.submissions;
DROP SCHEMA IF EXISTS execution;