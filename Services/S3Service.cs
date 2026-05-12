using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GMT.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly IConfiguration _configuration;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _bucketName = _configuration["AWS:S3BucketName"] ?? throw new ArgumentNullException("AWS:S3BucketName", "S3 bucket name is not configured in appsettings.json");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string key)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty", nameof(file));

            // If key not provided, generate a GUID-based key preserving extension
            if (string.IsNullOrWhiteSpace(key))
            {
                var extension = Path.GetExtension(file.FileName);
                key = $"{Guid.NewGuid():N}{extension}";
            }

            await using var stream = file.OpenReadStream();
            var uploadRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(uploadRequest);
            return key; // Return the key (can be used to retrieve URL)
        }

        public async Task<string> GetFileUrlAsync(string key, int expiresInHours = 24)
        {
            // Yield to make async
            await Task.Yield();
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(expiresInHours)
            };
            return _s3Client.GetPreSignedURL(request);
        }

        // Optional: Delete file
        public async Task DeleteFileAsync(string key)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };
            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
    }
}