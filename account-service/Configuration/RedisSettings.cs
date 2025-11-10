namespace AccountService.Configuration;

public class RedisSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string? Password { get; set; }
    public int DefaultDatabase { get; set; }
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
    public bool AbortOnConnectFail { get; set; }
}
