using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace WebApplicationBlobDEMO.Controllers
{


    [ApiController]
    [Route("[controller]")]
    public class BlobController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string ContainerName = "democontainer";

        public BlobController(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadBlob(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(file.FileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return Ok(new
            {
                Message = "Upload completed.",
                BlobUri = blobClient.Uri
            });
        }

        [HttpGet("download/{blobName}")]
        public async Task<IActionResult> DownloadBlob(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
                return NotFound($"Blob '{blobName}' not found.");

            BlobDownloadInfo download = await blobClient.DownloadAsync();

            return File(download.Content, download.ContentType, blobName);
        }

        [HttpDelete("{blobName}")]
        public async Task<IActionResult> DeleteBlob(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            bool existed = await blobClient.DeleteIfExistsAsync();

            if (!existed)
                return NotFound($"Blob '{blobName}' was not found.");

            return Ok(new { Message = "Delete completed.", BlobUri = blobClient.Uri });
        }

        [HttpGet("sas/{blobName}")]
        public IActionResult GetBlobSasUri(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!blobClient.CanGenerateSasUri)
            {
                return BadRequest("The configured BlobServiceClient cannot sign SAS tokens. Check your connection string or credentials.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                BlobName = blobName,
                Resource = "b", // "b" specifies a individual Blob SAS
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Clock skew buffer
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(3)   // SAS Token validity duration (3 hours)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            return Ok(new
            {
                BlobName = blobName,
                SasUrl = sasUri.ToString()
            });
        }
    }
}
