using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace DataLayerStorageUploadService
{
    public class Utils
    {
        private static string? _chiaRoot = null;
        private static readonly HttpClient client = new HttpClient();
        private static readonly string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static string GetChiaRootEnv()
        {
            string chiaRoot = Environment.GetEnvironmentVariable("CHIA_ROOT", EnvironmentVariableTarget.Machine);
            return chiaRoot;
        }

        public static string GetChiaRoot()
        {
            if (_chiaRoot == null)
            {
                var chiaRootEnv = GetChiaRootEnv();

                Logger.LogInformation("CHIA ROOT: " + chiaRootEnv);

                if (!string.IsNullOrEmpty(chiaRootEnv))
                {
                    _chiaRoot = Path.GetFullPath(chiaRootEnv);
                }
                else
                {
                    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    _chiaRoot = Path.GetFullPath(Path.Combine(homeDir, ".chia", "mainnet"));
                }
            }

            Logger.LogInformation("Using Chia Root at: " + _chiaRoot);

            return _chiaRoot;
        }

        public static string GenerateSourceFilePath(string chiaRoot, string file)
        {
            return Path.Combine(
                chiaRoot,
                "data_layer",
                "db",
                "server_files_location_mainnet",
                file
            );
        }

        public static bool MatchKey(Dictionary<string, object> json, string key)
        {
            return json.ContainsKey(key);
        }

        public static List<string> GetFilesBySubstring(string substring)
        {
            var chiaRoot = GetChiaRoot();
            var directory = Path.Combine(
                chiaRoot,
                "data_layer",
                "db",
                "server_files_location_mainnet"
            );
            var files = Directory.GetFiles(directory);
            var matchedFiles = files.Where(file => file.Contains(substring)).ToList();
            return matchedFiles;
        }

        public static string GetDLaaSRootEnv()
        {
            string root = "";
            string dlaasRoot = Environment.GetEnvironmentVariable("DL_STORAGE_ROOT", EnvironmentVariableTarget.Machine);

            if (!string.IsNullOrEmpty(dlaasRoot))
            {
                root = Path.GetFullPath(dlaasRoot);
            }
            else
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                root = Path.Combine(homeDir, ".dlaas");
            }
            return root;
        }

        public static Dictionary<string, object> GetConfig()
        {
            try
            {
                string persistenceFolderPath = GetDLaaSRootEnv();
                string configFilePath = Path.Combine(persistenceFolderPath, "config.yaml");

                Logger.LogInformation("Getting Service Config from: " + configFilePath);

                if (!File.Exists(configFilePath))
                {
                    try
                    {
                        if (!Directory.Exists(persistenceFolderPath))
                        {
                            Directory.CreateDirectory(persistenceFolderPath);
                        }

                        var defaultConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText("defaultConfig.json"));
                        var serializer = new Serializer();
                        File.WriteAllText(configFilePath, serializer.Serialize(defaultConfig));
                    }
                    catch (Exception)
                    {
                        return JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText("defaultConfig.json"));
                    }
                }

                try
                {
                    var deserializer = new Deserializer();
                    Logger.LogInformation(File.ReadAllText(configFilePath));
                    var yml = deserializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(configFilePath));
                    return yml;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Config file not found at {configFilePath}", e);
                    throw;
                }
            }
            catch (Exception e)
            {
                string persistenceFolderPath = GetDLaaSRootEnv();
                string configFilePath = Path.Combine(persistenceFolderPath, "config.yaml");
                Console.WriteLine($"Config file not found at {configFilePath}", e);
                throw;
            }
        }

        public static async Task<string> GetPresignedUrl(string storeId, string filename, int retry = 0)
        {
            var config = GetConfig();
            var username = config["CLIENT_ACCESS_KEY"].ToString();
            var password = config["CLIENT_SECRET_ACCESS_KEY"].ToString();

            try
            {
                var requestBody = new
                {
                    store_id = storeId,
                    filename = filename
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                var response = await client.PostAsync("https://api.datalayer.storage/file/v1/upload", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
                else
                {
                    throw new Exception($"Server responded with status code {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                if (retry < 5)
                {
                    await Task.Delay(1000);
                    return await GetPresignedUrl(storeId, filename, retry + 1);
                }
                else
                {
                    var errorMessage = ex.Message ?? "No message provided";
                    Logger.LogError($"Error getting presigned URL or uploading file: {filename}. Error: {errorMessage}");
                    throw new Exception("Error getting presigned URL");
                }
            }
        }


        public static async Task<HttpResponseMessage> UploadFileToS3(string storeId, string filename, int retry = 0)
        {
            try
            {
                var response = await GetPresignedUrl(storeId, filename);
                var responseObject = JsonConvert.DeserializeObject<dynamic>(response);

                if (responseObject.isDuplicate == true)
                {
                    Logger.LogInformation($"File already exists: {filename}");
                    return null;
                }

                if (responseObject.isDuplicate == false && responseObject.error == null)
                {
                    string chiaRoot = GetChiaRoot();
                    string filePath = GenerateSourceFilePath(chiaRoot, filename);

                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var content = new MultipartFormDataContent();
                        foreach (var field in responseObject.presignedPost.fields)
                        {
                            content.Add(new StringContent(field.Value.ToString()), field.Name.ToString());
                        }
                        content.Add(new StreamContent(fileStream), "\"file\"", filename);

                        // Remove Authorization header if it exists
                        if (client.DefaultRequestHeaders.Authorization != null)
                        {
                            client.DefaultRequestHeaders.Authorization = null;
                        }

                        var uploadResponse = await client.PostAsync(responseObject.presignedPost.url.ToString(), content);

                        if (uploadResponse.IsSuccessStatusCode)
                        {
                            Logger.LogInformation($"File successfully uploaded: {filename}");
                            return uploadResponse;
                        }
                        else
                        {
                            var errorMessage = await uploadResponse.Content.ReadAsStringAsync();
                            Logger.LogError($"Error uploading file: {filename}. Server responded with status code: {uploadResponse.StatusCode}. Error message: {errorMessage}");
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (retry < 4)
                {
                    await Task.Delay(1000);
                    return await UploadFileToS3(storeId, filename, retry + 1);
                }
                Logger.LogError($"Error uploading file after 5 attempts: {filename}");
            }
            throw new Exception($"Failed to upload file: {filename} after 5 attempts");
        }

        private static string GetStoreIdFromFilename(string filename)
        {
            var splits = filename.Split('-');
            if (splits.Length > 0)
            {
                return splits[0];
            }
            return null;
        }


        public static async Task<string> InitiateAddMissingFilesOnLocal()
        {
            try
            {
                var chiaRoot = GetChiaRoot();
                var directoryPath = Path.Combine(
                    chiaRoot,
                    "data_layer",
                    "db",
                    "server_files_location_mainnet"
                );

                var files = Directory.GetFiles(directoryPath);
                foreach (var filePath in files)
                {
                    var filename = Path.GetFileName(filePath);
                    var storeId = GetStoreIdFromFilename(filename);

                    // Perform necessary operations with the storeId and filename
                    await UploadFileToS3(storeId, filename);
                    await Task.Delay(1000);
                }

                return "Add missing files completed.";
            }
            catch (Exception ex)
            {
                // Handle the exception and log the error
                Logger.LogError("Error: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Logger.LogError("Inner Exception: " + ex.InnerException.Message);
                }
            }

            return null;
        }

    }
}
