// Controllers/PluginsController.cs
namespace DataLayerStorageUploadService.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    [ApiController]
    public class PluginController : ControllerBase
    {
        [HttpPost("add_missing_files")]
        public async Task<IActionResult> AddMissingFiles([FromBody] Requests.AddMissingFilesRequest request)
        {
            try
            {
                var storeId = request.store_id;
                var files = request.files;

                foreach (var file in files)
                {
                    if (!file.Contains("full"))
                    {
                        await Utils.UploadFileToS3(storeId, file);
                        await Task.Delay(1000);
                    }
                }

                return Ok(new { uploaded = true });
            }
            catch (Exception ex)
            {
                // Log or handle the error
                return StatusCode(500, new { uploaded = false });
            }
        }


        [HttpPost("handle_upload")]
        public IActionResult HandleUpload()
        {
            return Ok(new { handle_upload = true });
        }

        [HttpPost("plugin_info")]
        public IActionResult PluginInfo()
        {
            try
            {
                var info = new
                {
                    name = "S3 Plugin For Datalayer Storage",
                    version = "1.0.0",
                    description = "A plugin to handle upload for files to the Datalayer Storage System"
                };

                return Ok(info);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving plugin info: {ex.Message}");
                return StatusCode(500, new { error = "Failed to retrieve plugin information" });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromBody] Requests.UploadRequest request)
        {
            Logger.LogInformation("upload request received");
            var storeId = request.store_id;
            var fullTreeFilename = request.full_tree_filename;
            var diffFilename = request.diff_filename;

            try
            {
                await Task.WhenAll(
                    // Don't upload the full files for now
                    // UploadFileToS3(storeId, fullTreeFilename),
                    Utils.UploadFileToS3(storeId, diffFilename)
                );

                return Ok(new { uploaded = true });
            }
            catch (Exception ex)
            {
                Logger.LogError("Cannot upload file " + ex.Message);
                return BadRequest(new { uploaded = false });
            }
        }
    }
}
