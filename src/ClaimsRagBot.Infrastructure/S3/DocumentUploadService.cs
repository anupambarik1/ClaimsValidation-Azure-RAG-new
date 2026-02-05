using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using ClaimsRagBot.Core.Interfaces;
using ClaimsRagBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace ClaimsRagBot.Infrastructure.S3;

public class DocumentUploadService : IDocumentUploadService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _uploadPrefix;

    public DocumentUploadService(IConfiguration configuration)
    {
        var region = configuration["AWS:Region"] ?? "us-east-1";
        var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
        
        var accessKeyId = configuration["AWS:AccessKeyId"];
        var secretAccessKey = configuration["AWS:SecretAccessKey"];

        // accessKeyId = "testaccesskey";
        // secretAccessKey = "testsecretaccesskey";


        var config = new AmazonS3Config
        {
            RegionEndpoint = regionEndpoint
        };
        
        if (!string.IsNullOrEmpty(accessKeyId) && !string.IsNullOrEmpty(secretAccessKey))
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            _s3Client = new AmazonS3Client(credentials, config);
            Console.WriteLine($"[S3] Using credentials from appsettings for region: {region}");
        }
        else
        {
            // Fallback to default credential chain
            _s3Client = new AmazonS3Client(config);
            Console.WriteLine($"[S3] Using default credential chain for region: {region}");
        }
        
        _bucketName = configuration["AWS:S3:DocumentBucket"] ?? throw new InvalidOperationException("AWS:S3:DocumentBucket not configured");
        _uploadPrefix = configuration["AWS:S3:UploadPrefix"] ?? "uploads/";
    }

    public async Task<DocumentUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, string userId)
    {
        var documentId = Guid.NewGuid().ToString();
        var s3Key = $"{_uploadPrefix}{userId}/{documentId}/{fileName}";
        
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key,
                InputStream = fileStream,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                Metadata =
                {
                    ["document-id"] = documentId,
                    ["user-id"] = userId,
                    ["upload-timestamp"] = DateTime.UtcNow.ToString("O")
                }
            };
            
            var response = await _s3Client.PutObjectAsync(putRequest);
            
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Failed to upload document to S3. Status: {response.HttpStatusCode}");
            }
            
            Console.WriteLine($"[S3] Successfully uploaded document: {documentId} to {s3Key}");
            
            return new DocumentUploadResult(
                DocumentId: documentId,
                S3Bucket: _bucketName,
                S3Key: s3Key,
                ContentType: contentType,
                FileSize: fileStream.Length,
                UploadedAt: DateTime.UtcNow
            );
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"[S3] Error uploading document: {ex.ErrorCode} - {ex.Message}");
            throw new Exception($"S3 upload failed: {ex.ErrorCode} - {ex.Message}", ex);
        }
    }

    public async Task<Stream> DownloadAsync(string documentId)
    {
        try
        {
            // Search for the document by listing objects with the document ID prefix
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = _uploadPrefix
            };
            
            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);
            var matchingObject = listResponse.S3Objects.FirstOrDefault(obj => obj.Key.Contains(documentId));
            
            if (matchingObject == null)
            {
                throw new FileNotFoundException($"Document {documentId} not found in S3");
            }
            
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = matchingObject.Key
            };
            
            var response = await _s3Client.GetObjectAsync(getRequest);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"[S3] Error downloading document: {ex.ErrorCode} - {ex.Message}");
            throw new Exception($"S3 download failed: {ex.ErrorCode} - {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(string documentId)
    {
        try
        {
            // Find the document first
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = _uploadPrefix
            };
            
            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);
            var matchingObjects = listResponse.S3Objects.Where(obj => obj.Key.Contains(documentId)).ToList();
            
            foreach (var obj in matchingObjects)
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = obj.Key
                };
                
                await _s3Client.DeleteObjectAsync(deleteRequest);
                Console.WriteLine($"[S3] Deleted document: {obj.Key}");
            }
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"[S3] Error deleting document: {ex.ErrorCode} - {ex.Message}");
            throw new Exception($"S3 delete failed: {ex.ErrorCode} - {ex.Message}", ex);
        }
    }

    public async Task<bool> ExistsAsync(string documentId)
    {
        try
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = _uploadPrefix,
                MaxKeys = 100
            };
            
            var listResponse = await _s3Client.ListObjectsV2Async(listRequest);
            return listResponse.S3Objects.Any(obj => obj.Key.Contains(documentId));
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"[S3] Error checking document existence: {ex.ErrorCode} - {ex.Message}");
            return false;
        }
    }
}
