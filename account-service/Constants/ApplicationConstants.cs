namespace AccountService.Constants;

public static class ApplicationConstants
{
    public static class Defaults
    {
        public const int UserRating = 1500;
        public const int TokenExpirySeconds = 3600;
    }

    public static class Limits
    {
        public const int MaxErrorMessageLength = 2000;
        public const int OutboxBatchSize = 50;
        public const int MaxRetryExponent = 6;
    }

    public static class Intervals
    {
        public const int OutboxPollingSeconds = 5;
        public const int PolicySyncMinutes = 5;
        public const int NetworkRecoverySeconds = 10;
    }

    public static class TokenExpiry
    {
        public const int EmailVerificationHours = 24;
        public const int PasswordResetHours = 1;
        public const int RefreshTokenDays = 7;
    }

    public static class Thresholds
    {
        public const int ActiveUserDays = 30;
    }

    public static class Roles
    {
        public const string DefaultRole = "user";
        public const string Admin = "admin";
        public const string Setter = "setter";
    }

    public static class Queues
    {
        public const string UserEventsQueue = "content-service.user-events";
    }
}
