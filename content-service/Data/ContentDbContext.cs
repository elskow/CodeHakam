using ContentService.Models;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Data;

public class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    public DbSet<Problem> Problems { get; set; }
    public DbSet<TestCase> TestCases { get; set; }
    public DbSet<Editorial> Editorials { get; set; }
    public DbSet<Discussion> Discussions { get; set; }
    public DbSet<DiscussionComment> DiscussionComments { get; set; }
    public DbSet<ProblemTag> ProblemTags { get; set; }
    public DbSet<ProblemList> ProblemLists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("content");

        ConfigureProblem(modelBuilder);
        ConfigureTestCase(modelBuilder);
        ConfigureEditorial(modelBuilder);
        ConfigureDiscussion(modelBuilder);
        ConfigureDiscussionComment(modelBuilder);
        ConfigureProblemTag(modelBuilder);
        ConfigureProblemList(modelBuilder);
    }

    private static void ConfigureProblem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Problem>(entity =>
        {
            entity.ToTable("problems");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(p => p.Slug)
                .HasColumnName("slug")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.Description)
                .HasColumnName("description")
                .HasMaxLength(50000)
                .IsRequired();

            entity.Property(p => p.InputFormat)
                .HasColumnName("input_format")
                .HasMaxLength(10000)
                .IsRequired();

            entity.Property(p => p.OutputFormat)
                .HasColumnName("output_format")
                .HasMaxLength(10000)
                .IsRequired();

            entity.Property(p => p.Constraints)
                .HasColumnName("constraints")
                .HasMaxLength(10000)
                .IsRequired();

            entity.Property(p => p.Difficulty)
                .HasColumnName("difficulty")
                .IsRequired();

            entity.Property(p => p.TimeLimit)
                .HasColumnName("time_limit")
                .IsRequired();

            entity.Property(p => p.MemoryLimit)
                .HasColumnName("memory_limit")
                .IsRequired();

            entity.Property(p => p.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            entity.Property(p => p.Visibility)
                .HasColumnName("visibility")
                .IsRequired();

            entity.Property(p => p.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.Property(p => p.ViewCount)
                .HasColumnName("view_count")
                .HasDefaultValue(0);

            entity.Property(p => p.SubmissionCount)
                .HasColumnName("submission_count")
                .HasDefaultValue(0);

            entity.Property(p => p.AcceptedCount)
                .HasColumnName("accepted_count")
                .HasDefaultValue(0);

            entity.Property(p => p.AcceptanceRate)
                .HasColumnName("acceptance_rate")
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            entity.HasIndex(p => p.Slug)
                .IsUnique()
                .HasDatabaseName("ix_problems_slug");

            entity.HasIndex(p => p.Difficulty)
                .HasDatabaseName("ix_problems_difficulty");

            entity.HasIndex(p => p.AuthorId)
                .HasDatabaseName("ix_problems_author_id");

            entity.HasIndex(p => p.CreatedAt)
                .HasDatabaseName("ix_problems_created_at");

            entity.HasIndex(p => p.Visibility)
                .HasDatabaseName("ix_problems_visibility");
        });
    }

    private static void ConfigureTestCase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.ToTable("test_cases");
            entity.HasKey(tc => tc.Id);

            entity.Property(tc => tc.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(tc => tc.ProblemId)
                .HasColumnName("problem_id")
                .IsRequired();

            entity.Property(tc => tc.TestNumber)
                .HasColumnName("test_number")
                .IsRequired();

            entity.Property(tc => tc.IsSample)
                .HasColumnName("is_sample")
                .IsRequired();

            entity.Property(tc => tc.InputFileUrl)
                .HasColumnName("input_file_url")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(tc => tc.OutputFileUrl)
                .HasColumnName("output_file_url")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(tc => tc.InputSize)
                .HasColumnName("input_size")
                .IsRequired();

            entity.Property(tc => tc.OutputSize)
                .HasColumnName("output_size")
                .IsRequired();

            entity.Property(tc => tc.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            entity.Property(tc => tc.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.HasOne(tc => tc.Problem)
                .WithMany(p => p.TestCases)
                .HasForeignKey(tc => tc.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(tc => tc.ProblemId)
                .HasDatabaseName("ix_test_cases_problem_id");

            entity.HasIndex(tc => new { tc.ProblemId, tc.TestNumber })
                .HasDatabaseName("ix_test_cases_problem_test_number");
        });
    }

    private static void ConfigureEditorial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Editorial>(entity =>
        {
            entity.ToTable("editorials");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.ProblemId)
                .HasColumnName("problem_id")
                .IsRequired();

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .HasMaxLength(50000)
                .IsRequired();

            entity.Property(e => e.Approach)
                .HasColumnName("approach")
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(e => e.TimeComplexity)
                .HasColumnName("time_complexity")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.SpaceComplexity)
                .HasColumnName("space_complexity")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.SolutionCode)
                .HasColumnName("solution_code")
                .HasColumnType("jsonb");

            entity.Property(e => e.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            entity.Property(e => e.PublishedAt)
                .HasColumnName("published_at");

            entity.Property(e => e.IsPublished)
                .HasColumnName("is_published")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasOne(e => e.Problem)
                .WithOne(p => p.Editorial)
                .HasForeignKey<Editorial>(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProblemId)
                .IsUnique()
                .HasDatabaseName("ix_editorials_problem_id");
        });
    }

    private static void ConfigureDiscussion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Discussion>(entity =>
        {
            entity.ToTable("discussions");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(d => d.ProblemId)
                .HasColumnName("problem_id");

            entity.Property(d => d.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(d => d.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(d => d.Content)
                .HasColumnName("content")
                .HasMaxLength(10000)
                .IsRequired();

            entity.Property(d => d.VoteCount)
                .HasColumnName("vote_count")
                .HasDefaultValue(0);

            entity.Property(d => d.CommentCount)
                .HasColumnName("comment_count")
                .HasDefaultValue(0);

            entity.Property(d => d.IsLocked)
                .HasColumnName("is_locked")
                .HasDefaultValue(false);

            entity.Property(d => d.IsPinned)
                .HasColumnName("is_pinned")
                .HasDefaultValue(false);

            entity.Property(d => d.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(d => d.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasOne(d => d.Problem)
                .WithMany(p => p.Discussions)
                .HasForeignKey(d => d.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(d => d.ProblemId)
                .HasDatabaseName("ix_discussions_problem_id");

            entity.HasIndex(d => d.UserId)
                .HasDatabaseName("ix_discussions_user_id");

            entity.HasIndex(d => d.CreatedAt)
                .HasDatabaseName("ix_discussions_created_at");
        });
    }

    private static void ConfigureDiscussionComment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiscussionComment>(entity =>
        {
            entity.ToTable("discussion_comments");
            entity.HasKey(dc => dc.Id);

            entity.Property(dc => dc.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(dc => dc.DiscussionId)
                .HasColumnName("discussion_id")
                .IsRequired();

            entity.Property(dc => dc.ParentId)
                .HasColumnName("parent_id");

            entity.Property(dc => dc.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(dc => dc.Content)
                .HasColumnName("content")
                .HasMaxLength(5000)
                .IsRequired();

            entity.Property(dc => dc.VoteCount)
                .HasColumnName("vote_count")
                .HasDefaultValue(0);

            entity.Property(dc => dc.IsAccepted)
                .HasColumnName("is_accepted")
                .HasDefaultValue(false);

            entity.Property(dc => dc.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(dc => dc.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasOne(dc => dc.Discussion)
                .WithMany(d => d.Comments)
                .HasForeignKey(dc => dc.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dc => dc.Parent)
                .WithMany(dc => dc.Replies)
                .HasForeignKey(dc => dc.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(dc => dc.DiscussionId)
                .HasDatabaseName("ix_discussion_comments_discussion_id");

            entity.HasIndex(dc => dc.ParentId)
                .HasDatabaseName("ix_discussion_comments_parent_id");

            entity.HasIndex(dc => dc.UserId)
                .HasDatabaseName("ix_discussion_comments_user_id");
        });
    }

    private static void ConfigureProblemTag(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProblemTag>(entity =>
        {
            entity.ToTable("problem_tags");
            entity.HasKey(pt => new { pt.ProblemId, pt.Tag });

            entity.Property(pt => pt.ProblemId)
                .HasColumnName("problem_id")
                .IsRequired();

            entity.Property(pt => pt.Tag)
                .HasColumnName("tag")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(pt => pt.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.HasOne(pt => pt.Problem)
                .WithMany(p => p.Tags)
                .HasForeignKey(pt => pt.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pt => pt.Tag)
                .HasDatabaseName("ix_problem_tags_tag");
        });
    }

    private static void ConfigureProblemList(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProblemList>(entity =>
        {
            entity.ToTable("problem_lists");
            entity.HasKey(pl => pl.Id);

            entity.Property(pl => pl.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(pl => pl.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(pl => pl.Description)
                .HasColumnName("description")
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(pl => pl.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired();

            entity.Property(pl => pl.ProblemIds)
                .HasColumnName("problem_ids")
                .HasColumnType("bigint[]")
                .IsRequired();

            entity.Property(pl => pl.IsPublic)
                .HasColumnName("is_public")
                .HasDefaultValue(false);

            entity.Property(pl => pl.ViewCount)
                .HasColumnName("view_count")
                .HasDefaultValue(0);

            entity.Property(pl => pl.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(pl => pl.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.HasIndex(pl => pl.OwnerId)
                .HasDatabaseName("ix_problem_lists_owner_id");

            entity.HasIndex(pl => pl.IsPublic)
                .HasDatabaseName("ix_problem_lists_is_public");
        });
    }
}
