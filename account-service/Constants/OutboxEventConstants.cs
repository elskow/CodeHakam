namespace AccountService.Constants;

public static class OutboxEventConstants
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Published = "published";
    public const string Failed = "failed";
}
