using ContentService.Enums;
using ContentService.Models;

namespace ContentService.Tests.Helpers;

public static class TestDataBuilder
{
    public static Problem CreateProblem(
        long? id = null,
        string slug = "two-sum",
        string title = "Two Sum",
        Difficulty difficulty = Difficulty.Easy,
        int timeLimit = 1000,
        int memoryLimit = 262144,
        long authorId = 1,
        ProblemVisibility visibility = ProblemVisibility.Public,
        bool isActive = true)
    {
        var problem = new Problem
        {
            Slug = slug,
            Title = title,
            Description = "Given an array of integers, return indices of the two numbers such that they add up to a specific target.",
            InputFormat = "First line contains n and target. Second line contains n integers.",
            OutputFormat = "Two space-separated integers representing the indices.",
            Constraints = "2 <= n <= 10^4, -10^9 <= nums[i] <= 10^9",
            Difficulty = difficulty,
            TimeLimit = timeLimit,
            MemoryLimit = memoryLimit,
            AuthorId = authorId,
            Visibility = visibility,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ViewCount = 0,
            SubmissionCount = 0,
            AcceptedCount = 0,
            AcceptanceRate = 0
        };

        if (id.HasValue)
        {
            problem.Id = id.Value;
        }

        return problem;
    }

    public static TestCase CreateTestCase(
        long? id = null,
        long problemId = 1,
        int testNumber = 1,
        bool isSample = true,
        string inputFileUrl = "http://minio:9000/testcases/input1.txt",
        string outputFileUrl = "http://minio:9000/testcases/output1.txt",
        long inputSize = 1024,
        long outputSize = 512,
        bool isActive = true)
    {
        var testCase = new TestCase
        {
            ProblemId = problemId,
            TestNumber = testNumber,
            IsSample = isSample,
            InputFileUrl = inputFileUrl,
            OutputFileUrl = outputFileUrl,
            InputSize = inputSize,
            OutputSize = outputSize,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        if (id.HasValue)
        {
            testCase.Id = id.Value;
        }

        return testCase;
    }

    public static Editorial CreateEditorial(
        long? id = null,
        long problemId = 1,
        long authorId = 1,
        bool isPublished = false)
    {
        var editorial = new Editorial
        {
            ProblemId = problemId,
            Content = "This problem can be solved using a hash map to store the complement of each number.",
            Approach = "Hash Map",
            TimeComplexity = "O(n)",
            SpaceComplexity = "O(n)",
            SolutionCode = "{\"cpp\": \"vector<int> twoSum(vector<int>& nums, int target) { ... }\", \"python\": \"def two_sum(nums, target): ...\"}",
            AuthorId = authorId,
            IsPublished = isPublished,
            PublishedAt = isPublished ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (id.HasValue)
        {
            editorial.Id = id.Value;
        }

        return editorial;
    }

    public static Discussion CreateDiscussion(
        long? id = null,
        long? problemId = 1,
        long userId = 1,
        string title = "How to optimize this solution?",
        string content = "I am getting TLE on test case 5. Any suggestions?",
        bool isLocked = false,
        bool isPinned = false)
    {
        var discussion = new Discussion
        {
            ProblemId = problemId,
            UserId = userId,
            Title = title,
            Content = content,
            VoteCount = 0,
            CommentCount = 0,
            IsLocked = isLocked,
            IsPinned = isPinned,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (id.HasValue)
        {
            discussion.Id = id.Value;
        }

        return discussion;
    }

    public static DiscussionComment CreateDiscussionComment(
        long? id = null,
        long discussionId = 1,
        long? parentId = null,
        long userId = 1,
        string content = "Try using a hash map instead of nested loops.",
        bool isAccepted = false)
    {
        var comment = new DiscussionComment
        {
            DiscussionId = discussionId,
            ParentId = parentId,
            UserId = userId,
            Content = content,
            VoteCount = 0,
            IsAccepted = isAccepted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (id.HasValue)
        {
            comment.Id = id.Value;
        }

        return comment;
    }

    public static ProblemTag CreateProblemTag(
        long problemId,
        string tag = "array")
    {
        return new ProblemTag
        {
            ProblemId = problemId,
            Tag = tag.ToLower(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ProblemList CreateProblemList(
        long? id = null,
        long ownerId = 1,
        string title = "Top Interview Questions",
        string description = "Must-solve problems for interviews",
        long[]? problemIds = null,
        bool isPublic = false)
    {
        var list = new ProblemList
        {
            Title = title,
            Description = description,
            OwnerId = ownerId,
            ProblemIds = problemIds ?? [],
            IsPublic = isPublic,
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (id.HasValue)
        {
            list.Id = id.Value;
        }

        return list;
    }
}
