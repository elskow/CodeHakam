package services

import (
	"context"
	"fmt"

	"execution_service/internal/cache"
	"execution_service/internal/database"
	"execution_service/internal/models"
)

type LanguageService struct {
	db    *database.DB
	cache *cache.ValkeyClient
}

func NewLanguageService(db *database.DB, cache *cache.ValkeyClient) *LanguageService {
	return &LanguageService{
		db:    db,
		cache: cache,
	}
}

func (ls *LanguageService) GetSupportedLanguages(ctx context.Context) ([]models.SupportedLanguage, error) {
	// Get from database
	languages, err := ls.db.GetSupportedLanguages(ctx)
	if err != nil {
		return nil, fmt.Errorf("failed to get supported languages: %w", err)
	}

	// Cache each language individually
	for _, language := range languages {
		ls.cache.CacheLanguage(ctx, language.LanguageCode, &language)
	}

	return languages, nil
}

func (ls *LanguageService) GetLanguage(ctx context.Context, code string) (*models.SupportedLanguage, error) {
	// Try cache first
	language, err := ls.cache.GetCachedLanguage(ctx, code)
	if err == nil {
		return language, nil
	}

	// Get from database
	language, err = ls.db.GetLanguage(ctx, code)
	if err != nil {
		return nil, fmt.Errorf("failed to get language: %w", err)
	}

	// Cache for 24 hours
	ls.cache.CacheLanguage(ctx, code, language)

	return language, nil
}

func (ls *LanguageService) UpdateLanguage(ctx context.Context, language *models.SupportedLanguage) error {
	// Update in database
	// TODO: Implement database update method

	// Invalidate cache
	ls.cache.InvalidateLanguage(ctx, language.LanguageCode)

	return nil
}

func (ls *LanguageService) GetLanguageConfig(code string) (*models.SupportedLanguage, error) {
	ctx := context.Background()
	language, err := ls.GetLanguage(ctx, code)
	if err != nil {
		// Fallback to hardcoded configs if database fails
		return ls.getHardcodedLanguageConfig(code), nil
	}
	return language, nil
}

func (ls *LanguageService) getHardcodedLanguageConfig(code string) *models.SupportedLanguage {
	configs := map[string]models.SupportedLanguage{
		"cpp": {
			LanguageCode:   "cpp",
			LanguageName:   "C++17",
			Version:        "17",
			CompileCommand: stringPtr("g++ -O2 -std=c++17 -o {output} {input}"),
			ExecuteCommand: "./{executable}",
			IsEnabled:      true,
		},
		"c": {
			LanguageCode:   "c",
			LanguageName:   "C11",
			Version:        "11",
			CompileCommand: stringPtr("gcc -O2 -std=c11 -o {output} {input}"),
			ExecuteCommand: "./{executable}",
			IsEnabled:      true,
		},
		"java": {
			LanguageCode:   "java",
			LanguageName:   "Java 17",
			Version:        "17",
			CompileCommand: stringPtr("javac {input}"),
			ExecuteCommand: "java {classname}",
			IsEnabled:      true,
		},
		"python": {
			LanguageCode:   "python",
			LanguageName:   "Python 3",
			Version:        "3.10",
			CompileCommand: nil,
			ExecuteCommand: "python3 {input}",
			IsEnabled:      true,
		},
		"go": {
			LanguageCode:   "go",
			LanguageName:   "Go 1.21",
			Version:        "1.21",
			CompileCommand: stringPtr("go build -o {output} {input}"),
			ExecuteCommand: "./{executable}",
			IsEnabled:      true,
		},
	}

	if config, exists := configs[code]; exists {
		return &config
	}

	return &models.SupportedLanguage{
		LanguageCode:   "python",
		LanguageName:   "Python 3",
		Version:        "3.10",
		CompileCommand: nil,
		ExecuteCommand: "python3 {input}",
		IsEnabled:      true,
	}
}

func stringPtr(s string) *string {
	return &s
}
