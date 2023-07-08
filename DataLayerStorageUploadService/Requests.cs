using Newtonsoft.Json;
using System.Collections.Generic;

namespace DataLayerStorageUploadService
{
    public class Requests
    {
        public class AddMissingFilesRequest
        {
            [JsonProperty("store_id")]
            public string store_id { get; set; }

            [JsonProperty("files")]
            public List<string> files { get; set; }
        }

        public class UploadRequest
        {
            [JsonProperty("store_id")]
            public string store_id { get; set; }

            [JsonProperty("full_tree_filename")]
            public string full_tree_filename { get; set; }

            [JsonProperty("diff_filename")]
            public string diff_filename { get; set; }
        }
    }
}
