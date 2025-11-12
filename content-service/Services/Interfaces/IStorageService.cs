namespace ContentService.Services.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(
        string bucketName,
        string objectName,
        Stream data,
        long size,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadFileAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default);

    Task<bool> FileExistsAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default);

    Task<long> GetFileSizeAsync(
        string bucketName,
        string objectName,
        CancellationToken cancellationToken = default);

    Task EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default);
}
