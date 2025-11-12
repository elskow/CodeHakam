namespace ContentService.Services.Implementations;

using ContentService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

public class MinIOStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinIOStorageService> _logger;

    public MinIOStorageService(
        IConfiguration configuration,
        ILogger<MinIOStorageService> logger)
    {
        _logger = logger;

        var endpoint = configuration["MinIO:Endpoint"] ?? "localhost:9000";
        var accessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["MinIO:SecretKey"] ?? "minioadmin";
        var useSSL = bool.Parse(configuration["MinIO:UseSSL"] ?? "false");

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(useSSL)
            .Build();

        _logger.LogInformation("MinIO client initialized for endpoint {Endpoint}", endpoint);
    }

    public async Task<string> UploadFileAsync(
        string bucketName,
        string objectName,
        Stream data,
        long size,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(bucketName, cancellationToken);

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

        _logger.LogInformation("Uploaded file {ObjectName} to bucket {BucketName}", objectName, bucketName);

        return objectName;
    }

    public async Task<Stream> DownloadFileAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
            });

        await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

        _logger.LogInformation("Downloaded file {ObjectName} from bucket {BucketName}", objectName, bucketName);

        return memoryStream;
    }

    public async Task DeleteFileAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName);

        await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

        _logger.LogInformation("Deleted file {ObjectName} from bucket {BucketName}", objectName, bucketName);
    }

    public async Task<bool> FileExistsAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<long> GetFileSizeAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        var statObjectArgs = new StatObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName);

        var stat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);

        return stat.Size;
    }

    public async Task EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        var beArgs = new BucketExistsArgs()
            .WithBucket(bucketName);

        bool found = await _minioClient.BucketExistsAsync(beArgs, cancellationToken);

        if (!found)
        {
            var mbArgs = new MakeBucketArgs()
                .WithBucket(bucketName);

            await _minioClient.MakeBucketAsync(mbArgs, cancellationToken);

            _logger.LogInformation("Created bucket {BucketName}", bucketName);
        }
    }
}
