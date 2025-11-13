namespace ContentService.Configuration;

public class ContentServiceSettings
{
    public long MaxTestCaseFileSize { get; set; }
    public int MaxProblemDescriptionLength { get; set; }
    public int DefaultPageSize { get; set; }
    public int MaxPageSize { get; set; }
    public string[] SupportedImageFormats { get; set; } = [];
    public long MaxImageSize { get; set; }
}

public class MinIOSettings
{
    public required string Endpoint { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required string BucketName { get; set; }
    public bool UseSSL { get; set; }
}

public class RabbitMQSettings
{
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string VirtualHost { get; set; } = "/";
    public required string ExchangeName { get; set; }
    public string ExchangeType { get; set; } = "topic";
}

public class JwtSettings
{
    public required string SecretKey { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int ExpiryMinutes { get; set; }
    public int RefreshTokenExpiryDays { get; set; }
}
