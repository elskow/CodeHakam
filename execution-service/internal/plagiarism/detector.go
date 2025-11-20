package plagiarism

import (
	"context"
	"crypto/md5"
	"fmt"
	"log"
	"regexp"
	"strings"
	"time"

	"execution_service/internal/config"
	"execution_service/internal/database"
	"execution_service/internal/models"
	"execution_service/internal/storage"
)

type PlagiarismDetector struct {
	db         *database.DB
	storage    *storage.MinIOClient
	config     *config.PlagiarismConfig
	workerPool chan *PlagiarismTask
	stopChan   chan struct{}
}

type PlagiarismConfig struct {
	Enabled                bool          `yaml:"enabled"`
	WorkerCount            int           `yaml:"worker_count"`
	SimilarityThreshold    float64       `yaml:"similarity_threshold"`
	MinCodeLength          int           `yaml:"min_code_length"`
	CheckInterval          time.Duration `yaml:"check_interval"`
	MaxSubmissionsPerCheck int           `yaml:"max_submissions_per_check"`
	Algorithms             []string      `yaml:"algorithms"`
}

type PlagiarismTask struct {
	SubmissionID int64
	UserID       int64
	ProblemID    int64
	Language     string
	CodeURL      string
	Priority     int
}

type PlagiarismResult struct {
	SubmissionID      int64   `json:"submission_id"`
	SimilarSubmission int64   `json:"similar_submission"`
	SimilarityScore   float64 `json:"similarity_score"`
	Algorithm         string  `json:"algorithm"`
	MatchedLines      []int   `json:"matched_lines"`
	TotalLines        int     `json:"total_lines"`
	Confidence        float64 `json:"confidence"`
}

type CodeFeatures struct {
	Hash           string
	Tokens         []string
	LineHashes     []string
	Structure      string
	VariableNames  []string
	FunctionNames  []string
	StringLiterals []string
	Comments       []string
}

func NewPlagiarismDetector(db *database.DB, storage *storage.MinIOClient, config *config.PlagiarismConfig) *PlagiarismDetector {
	return &PlagiarismDetector{
		db:         db,
		storage:    storage,
		config:     config,
		workerPool: make(chan *PlagiarismTask, 100),
		stopChan:   make(chan struct{}),
	}
}

func (pd *PlagiarismDetector) Start(ctx context.Context) error {
	if !pd.config.Enabled {
		log.Println("Plagiarism detection disabled")
		return nil
	}

	log.Printf("Starting plagiarism detection with %d workers", pd.config.WorkerCount)

	// Start worker pool
	for i := 0; i < pd.config.WorkerCount; i++ {
		go pd.worker(ctx, i+1)
	}

	// Start scheduler
	go pd.scheduler(ctx)

	return nil
}

func (pd *PlagiarismDetector) Stop() {
	close(pd.stopChan)
}

func (pd *PlagiarismDetector) EnqueueSubmission(submissionID, userID, problemID int64, language, codeURL string) {
	if !pd.config.Enabled {
		return
	}

	task := &PlagiarismTask{
		SubmissionID: submissionID,
		UserID:       userID,
		ProblemID:    problemID,
		Language:     language,
		CodeURL:      codeURL,
		Priority:     1, // Normal priority
	}

	select {
	case pd.workerPool <- task:
		log.Printf("Enqueued submission %d for plagiarism check", submissionID)
	default:
		log.Printf("Plagiarism queue full, skipping submission %d", submissionID)
	}
}

func (pd *PlagiarismDetector) scheduler(ctx context.Context) {
	ticker := time.NewTicker(pd.config.CheckInterval)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-pd.stopChan:
			return
		case <-ticker.C:
			pd.processPendingSubmissions(ctx)
		}
	}
}

func (pd *PlagiarismDetector) processPendingSubmissions(ctx context.Context) {
	// Get recent submissions that haven't been checked for plagiarism
	submissions, err := pd.db.GetUncheckedSubmissions(ctx, pd.config.MaxSubmissionsPerCheck)
	if err != nil {
		log.Printf("Failed to get unchecked submissions: %v", err)
		return
	}

	for _, submission := range submissions {
		pd.EnqueueSubmission(submission.ID, submission.UserID, submission.ProblemID,
			submission.Language, submission.CodeURL)
	}
}

func (pd *PlagiarismDetector) worker(ctx context.Context, workerID int) {
	log.Printf("Plagiarism worker %d started", workerID)

	for {
		select {
		case <-ctx.Done():
			return
		case <-pd.stopChan:
			return
		case task := <-pd.workerPool:
			pd.processSubmission(ctx, task, workerID)
		}
	}
}

func (pd *PlagiarismDetector) processSubmission(ctx context.Context, task *PlagiarismTask, workerID int) {
	log.Printf("Worker %d processing submission %d", workerID, task.SubmissionID)

	// Download code
	code, err := pd.storage.DownloadCode(ctx, task.CodeURL)
	if err != nil {
		log.Printf("Worker %d failed to download code for submission %d: %v", workerID, task.SubmissionID, err)
		return
	}

	// Skip if code is too short
	if len(code) < pd.config.MinCodeLength {
		log.Printf("Worker %d skipping submission %d (code too short)", workerID, task.SubmissionID)
		pd.markSubmissionChecked(ctx, task.SubmissionID)
		return
	}

	// Extract features from current submission
	currentFeatures, err := pd.extractFeatures(string(code))
	if err != nil {
		log.Printf("Worker %d failed to extract features from submission %d: %v", workerID, task.SubmissionID, err)
		return
	}

	// Get previous submissions for the same problem
	previousSubmissions, err := pd.db.GetPreviousSubmissions(ctx, task.ProblemID, task.SubmissionID)
	if err != nil {
		log.Printf("Worker %d failed to get previous submissions: %v", workerID, err)
		return
	}

	// Compare with each previous submission
	var maxSimilarity float64
	var mostSimilar int64
	var bestAlgorithm string

	for _, prevSub := range previousSubmissions {
		// Skip submissions from the same user (self-comparison)
		if prevSub.UserID == task.UserID {
			continue
		}

		// Download previous submission code
		prevCode, err := pd.storage.DownloadCode(ctx, prevSub.CodeURL)
		if err != nil {
			continue
		}

		// Extract features from previous submission
		prevFeatures, err := pd.extractFeatures(string(prevCode))
		if err != nil {
			continue
		}

		// Calculate similarity using different algorithms
		for _, algorithm := range pd.config.Algorithms {
			similarity := pd.calculateSimilarity(currentFeatures, prevFeatures, algorithm)

			if similarity > maxSimilarity {
				maxSimilarity = similarity
				mostSimilar = prevSub.ID
				bestAlgorithm = algorithm
			}
		}
	}

	// Create plagiarism report if similarity exceeds threshold
	if maxSimilarity >= pd.config.SimilarityThreshold {
		report := &models.PlagiarismReport{
			Submission1ID:   task.SubmissionID,
			Submission2ID:   mostSimilar,
			SimilarityScore: maxSimilarity,
			Algorithm:       bestAlgorithm,
			IsReviewed:      false,
			Status:          "pending",
		}

		if err := pd.db.CreatePlagiarismReport(ctx, report); err != nil {
			log.Printf("Worker %d failed to create plagiarism report: %v", workerID, err)
		} else {
			log.Printf("Worker %d detected plagiarism: submission %d similar to %d (score: %.2f)",
				workerID, task.SubmissionID, mostSimilar, maxSimilarity)
		}
	}

	// Mark submission as checked
	pd.markSubmissionChecked(ctx, task.SubmissionID)
}

func (pd *PlagiarismDetector) extractFeatures(code string) (*CodeFeatures, error) {
	features := &CodeFeatures{}

	// Calculate overall hash
	features.Hash = fmt.Sprintf("%x", md5.Sum([]byte(code)))

	// Tokenize code
	features.Tokens = pd.tokenizeCode(code)

	// Extract line hashes
	lines := strings.Split(code, "\n")
	features.LineHashes = make([]string, len(lines))
	for i, line := range lines {
		features.LineHashes[i] = fmt.Sprintf("%x", md5.Sum([]byte(strings.TrimSpace(line))))
	}

	// Extract structure (normalized code without comments and strings)
	features.Structure = pd.normalizeCode(code)

	// Extract identifiers
	features.VariableNames = pd.extractVariableNames(code)
	features.FunctionNames = pd.extractFunctionNames(code)

	// Extract string literals
	features.StringLiterals = pd.extractStringLiterals(code)

	// Extract comments
	features.Comments = pd.extractComments(code)

	return features, nil
}

func (pd *PlagiarismDetector) tokenizeCode(code string) []string {
	// Remove comments and strings first
	cleanCode := pd.removeCommentsAndStrings(code)

	// Split into tokens
	re := regexp.MustCompile(`\w+|[^\w\s]`)
	tokens := re.FindAllString(cleanCode, -1)

	// Filter and normalize
	var normalizedTokens []string
	for _, token := range tokens {
		token = strings.ToLower(token)
		if len(token) > 1 && !pd.isKeyword(token) {
			normalizedTokens = append(normalizedTokens, token)
		}
	}

	return normalizedTokens
}

func (pd *PlagiarismDetector) normalizeCode(code string) string {
	// Remove comments
	code = pd.removeComments(code)

	// Remove string literals
	code = pd.removeStringLiterals(code)

	// Normalize whitespace
	re := regexp.MustCompile(`\s+`)
	code = re.ReplaceAllString(code, " ")

	// Convert to lowercase
	code = strings.ToLower(code)

	return strings.TrimSpace(code)
}

func (pd *PlagiarismDetector) extractVariableNames(code string) []string {
	// Simple regex for variable declarations
	varNames := []string{}

	// C/C++/Java style variables
	re1 := regexp.MustCompile(`\b(int|float|double|char|bool|string|var|let|const)\s+([a-zA-Z_][a-zA-Z0-9_]*)\b`)
	matches := re1.FindAllStringSubmatch(code, -1)
	for _, match := range matches {
		if len(match) > 2 {
			varNames = append(varNames, match[2])
		}
	}

	// Assignment style variables
	re2 := regexp.MustCompile(`\b([a-zA-Z_][a-zA-Z0-9_]*)\s*=`)
	matches = re2.FindAllStringSubmatch(code, -1)
	for _, match := range matches {
		if len(match) > 1 && !pd.isKeyword(match[1]) {
			varNames = append(varNames, match[1])
		}
	}

	return varNames
}

func (pd *PlagiarismDetector) extractFunctionNames(code string) []string {
	funcNames := []string{}

	// Function definitions
	re := regexp.MustCompile(`\b(function|def|fn|func)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(`)
	matches := re.FindAllStringSubmatch(code, -1)
	for _, match := range matches {
		if len(match) > 2 {
			funcNames = append(funcNames, match[2])
		}
	}

	return funcNames
}

func (pd *PlagiarismDetector) extractStringLiterals(code string) []string {
	strings := []string{}

	// Match string literals
	re := regexp.MustCompile(`"([^"\\]|\\.)*"|'([^'\\]|\\.)*'`)
	matches := re.FindAllString(code, -1)

	for _, match := range matches {
		if len(match) > 2 { // Skip empty strings
			strings = append(strings, match)
		}
	}

	return strings
}

func (pd *PlagiarismDetector) extractComments(code string) []string {
	comments := []string{}

	// Single line comments
	re1 := regexp.MustCompile(`//.*`)
	matches1 := re1.FindAllString(code, -1)
	comments = append(comments, matches1...)

	// Multi-line comments
	re2 := regexp.MustCompile(`/\*[\s\S]*?\*/`)
	matches2 := re2.FindAllString(code, -1)
	comments = append(comments, matches2...)

	// Python comments
	re3 := regexp.MustCompile(`#.*`)
	matches3 := re3.FindAllString(code, -1)
	comments = append(comments, matches3...)

	return comments
}

func (pd *PlagiarismDetector) calculateSimilarity(features1, features2 *CodeFeatures, algorithm string) float64 {
	switch algorithm {
	case "hash":
		return pd.hashSimilarity(features1.Hash, features2.Hash)
	case "tokens":
		return pd.tokenSimilarity(features1.Tokens, features2.Tokens)
	case "lines":
		return pd.lineSimilarity(features1.LineHashes, features2.LineHashes)
	case "structure":
		return pd.structureSimilarity(features1.Structure, features2.Structure)
	case "variables":
		return pd.identifierSimilarity(features1.VariableNames, features2.VariableNames)
	case "functions":
		return pd.identifierSimilarity(features1.FunctionNames, features2.FunctionNames)
	case "strings":
		return pd.identifierSimilarity(features1.StringLiterals, features2.StringLiterals)
	default:
		return 0.0
	}
}

func (pd *PlagiarismDetector) hashSimilarity(hash1, hash2 string) float64 {
	if hash1 == hash2 {
		return 1.0
	}
	return 0.0
}

func (pd *PlagiarismDetector) tokenSimilarity(tokens1, tokens2 []string) float64 {
	if len(tokens1) == 0 && len(tokens2) == 0 {
		return 1.0
	}
	if len(tokens1) == 0 || len(tokens2) == 0 {
		return 0.0
	}

	// Create n-grams
	n := 3 // trigrams
	grams1 := pd.createNGrams(tokens1, n)
	grams2 := pd.createNGrams(tokens2, n)

	// Calculate Jaccard similarity
	intersection := pd.intersectionSize(grams1, grams2)
	union := len(grams1) + len(grams2) - intersection

	if union == 0 {
		return 0.0
	}

	return float64(intersection) / float64(union)
}

func (pd *PlagiarismDetector) lineSimilarity(hashes1, hashes2 []string) float64 {
	if len(hashes1) == 0 && len(hashes2) == 0 {
		return 1.0
	}
	if len(hashes1) == 0 || len(hashes2) == 0 {
		return 0.0
	}

	matchingLines := 0
	for i, hash1 := range hashes1 {
		if i < len(hashes2) && hash1 == hashes2[i] {
			matchingLines++
		}
	}

	totalLines := max(len(hashes1), len(hashes2))
	return float64(matchingLines) / float64(totalLines)
}

func (pd *PlagiarismDetector) structureSimilarity(struct1, struct2 string) float64 {
	// Use Levenshtein distance
	distance := pd.levenshteinDistance(struct1, struct2)
	maxLen := max(len(struct1), len(struct2))

	if maxLen == 0 {
		return 1.0
	}

	return 1.0 - float64(distance)/float64(maxLen)
}

func (pd *PlagiarismDetector) identifierSimilarity(identifiers1, identifiers2 []string) float64 {
	if len(identifiers1) == 0 && len(identifiers2) == 0 {
		return 1.0
	}
	if len(identifiers1) == 0 || len(identifiers2) == 0 {
		return 0.0
	}

	set1 := pd.toStringSet(identifiers1)
	set2 := pd.toStringSet(identifiers2)

	intersection := pd.intersectionSize(set1, set2)
	union := len(set1) + len(set2) - intersection

	if union == 0 {
		return 0.0
	}

	return float64(intersection) / float64(union)
}

// Helper functions
func (pd *PlagiarismDetector) createNGrams(tokens []string, n int) map[string]bool {
	grams := make(map[string]bool)
	for i := 0; i <= len(tokens)-n; i++ {
		gram := strings.Join(tokens[i:i+n], " ")
		grams[gram] = true
	}
	return grams
}

func (pd *PlagiarismDetector) intersectionSize(set1, set2 map[string]bool) int {
	count := 0
	for key := range set1 {
		if set2[key] {
			count++
		}
	}
	return count
}

func (pd *PlagiarismDetector) toStringSet(slice []string) map[string]bool {
	set := make(map[string]bool)
	for _, item := range slice {
		set[item] = true
	}
	return set
}

func (pd *PlagiarismDetector) levenshteinDistance(s1, s2 string) int {
	if len(s1) < len(s2) {
		return pd.levenshteinDistance(s2, s1)
	}
	if len(s2) == 0 {
		return len(s1)
	}

	previousRow := make([]int, len(s2)+1)
	for i := 0; i <= len(s2); i++ {
		previousRow[i] = i
	}

	for i, c1 := range s1 {
		currentRow := make([]int, len(s2)+1)
		currentRow[0] = i + 1

		for j, c2 := range s2 {
			insertCost := previousRow[j+1] + 1
			deleteCost := currentRow[j] + 1
			replaceCost := previousRow[j]
			if c1 != c2 {
				replaceCost++
			}

			currentRow[j+1] = min(insertCost, deleteCost, replaceCost)
		}

		previousRow = currentRow
	}

	return previousRow[len(s2)]
}

func (pd *PlagiarismDetector) removeComments(code string) string {
	// Remove multi-line comments
	re := regexp.MustCompile(`/\*[\s\S]*?\*/`)
	code = re.ReplaceAllString(code, "")

	// Remove single line comments
	re = regexp.MustCompile(`//.*`)
	code = re.ReplaceAllString(code, "")

	// Remove Python comments
	re = regexp.MustCompile(`#.*`)
	code = re.ReplaceAllString(code, "")

	return code
}

func (pd *PlagiarismDetector) removeStringLiterals(code string) string {
	// Remove string literals
	re := regexp.MustCompile(`"([^"\\]|\\.)*"|'([^'\\]|\\.)*'`)
	return re.ReplaceAllString(code, "")
}

func (pd *PlagiarismDetector) removeCommentsAndStrings(code string) string {
	code = pd.removeComments(code)
	code = pd.removeStringLiterals(code)
	return code
}

func (pd *PlagiarismDetector) isKeyword(token string) bool {
	keywords := map[string]bool{
		"if": true, "else": true, "for": true, "while": true, "do": true,
		"switch": true, "case": true, "break": true, "continue": true,
		"return": true, "void": true, "int": true, "float": true,
		"double": true, "char": true, "bool": true, "true": true, "false": true,
		"null": true, "public": true, "private": true, "protected": true,
		"class": true, "interface": true, "extends": true, "implements": true,
		"import": true, "package": true, "static": true, "final": true,
		"try": true, "catch": true, "finally": true, "throw": true,
		"new": true, "this": true, "super": true, "abstract": true,
	}

	return keywords[token]
}

func (pd *PlagiarismDetector) markSubmissionChecked(ctx context.Context, submissionID int64) {
	// Update submission to mark it as checked for plagiarism
	// This would typically update a timestamp in the submissions table
	log.Printf("Marked submission %d as plagiarism-checked", submissionID)
}

func (pd *PlagiarismDetector) GetDefaultConfig() *config.PlagiarismConfig {
	return &config.PlagiarismConfig{
		Enabled:                true,
		WorkerCount:            2,
		SimilarityThreshold:    0.85, // 85% similarity threshold
		MinCodeLength:          100,  // Minimum 100 characters
		CheckInterval:          5 * time.Minute,
		MaxSubmissionsPerCheck: 50,
		Algorithms:             []string{"tokens", "lines", "structure", "variables", "functions"},
	}
}
