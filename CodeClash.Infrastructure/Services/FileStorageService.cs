using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CodeClash.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CodeClash.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;

    public FileStorageService(IConfiguration configuration)
    {
        var cloudName = configuration["CloudinarySettings:CloudName"];
        var apiKey = configuration["CloudinarySettings:ApiKey"];
        var apiSecret = configuration["CloudinarySettings:ApiSecret"];

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string folderName,
        CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription(fileName, fileStream),
            Folder = $"codeclash/{folderName}"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.ToString();
    }

    public void DeleteFile(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;

        var publicId = GetPublicIdFromUrl(fileUrl);
        if (!string.IsNullOrEmpty(publicId))
        {
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };
            
            _cloudinary.Destroy(deletionParams);
        }
    }

    private string? GetPublicIdFromUrl(string url)
    {
        try
        {
            int uploadIndex = url.IndexOf("upload/");
            if (uploadIndex == -1) return null;

            string subPath = url.Substring(uploadIndex + 7);

            int firstSlash = subPath.IndexOf('/');
            if (firstSlash != -1)
            {
                string firstPart = subPath.Substring(0, firstSlash);
                if (firstPart.StartsWith("v") && long.TryParse(firstPart.Substring(1), out _))
                {
                    subPath = subPath.Substring(firstSlash + 1);
                }
            }

            int lastDot = subPath.LastIndexOf('.');
            if (lastDot != -1)
            {
                subPath = subPath.Substring(0, lastDot);
            }

            return subPath;
        }
        catch
        {
            return null;
        }
    }
}
