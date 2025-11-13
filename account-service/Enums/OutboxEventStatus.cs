namespace AccountService.Enums;

public enum OutboxEventStatus
{
    Pending,
    Processing,
    Published,
    Failed
}
