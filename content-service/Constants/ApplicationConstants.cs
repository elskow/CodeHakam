namespace ContentService.Constants;

public static class ApplicationConstants
{
    public static class Limits
    {
        public const long MaxTestCaseFileSizeBytes = 10 * 1024 * 1024; // 10 MB
        public const int MaxTestNumber = 1000;
        public const int MaxRetryCount = 5;
    }

    public static class Intervals
    {
        public const int NetworkRecoverySeconds = 10;
    }

    public static class Validation
    {
        public const int MaxTestCaseFileSizeMb = 10;
    }
}
