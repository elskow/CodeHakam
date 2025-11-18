package main

import (
	"database/sql"
	"embed"
	"log"
	"os"

	"execution_service/internal/config"

	_ "github.com/lib/pq"
	"github.com/pressly/goose/v3"
)

//go:embed migrations/*.sql
var embedFS embed.FS

func main() {
	if len(os.Args) < 2 {
		log.Fatal("Usage: migrate <command> [args]")
	}

	command := os.Args[1]

	cfg, err := config.Load()
	if err != nil {
		log.Fatalf("Failed to load config: %v", err)
	}

	db, err := sql.Open("postgres", cfg.Database.URL)
	if err != nil {
		log.Fatalf("Failed to connect to database: %v", err)
	}
	defer db.Close()

	if err := goose.SetDialect("postgres"); err != nil {
		log.Fatalf("Failed to set dialect: %v", err)
	}

	goose.SetBaseFS(embedFS)

	switch command {
	case "up":
		if err := goose.Up(db, "migrations"); err != nil {
			log.Fatalf("Failed to run migrations: %v", err)
		}
		log.Println("Migrations completed successfully")
	case "down":
		if err := goose.Down(db, "migrations"); err != nil {
			log.Fatalf("Failed to rollback migration: %v", err)
		}
		log.Println("Rollback completed successfully")
	case "status":
		if err := goose.Status(db, "migrations"); err != nil {
			log.Fatalf("Failed to get migration status: %v", err)
		}
	case "create":
		if len(os.Args) < 3 {
			log.Fatal("Usage: migrate create <migration_name>")
		}
		migrationName := os.Args[2]
		if err := goose.Create(db, "migrations", migrationName, "sql"); err != nil {
			log.Fatalf("Failed to create migration: %v", err)
		}
		log.Printf("Migration %s created successfully", migrationName)
	default:
		log.Fatalf("Unknown command: %s", command)
	}
}
