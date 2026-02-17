namespace ClaimsRagBot.Core.Models;

public record DocumentUploadResult(
    string DocumentId,
    string? S3Bucket,
    string? S3Key,
    string ContentType,
    long FileSize,
    DateTime UploadedAt,
    string? ContainerName = null,
    string? BlobName = null
);
