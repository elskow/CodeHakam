namespace AccountService.Configuration;

public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "codehakam.events";
    public bool Durable { get; set; } = true;
    public int ConnectionRetryCount { get; set; } = 5;
}
