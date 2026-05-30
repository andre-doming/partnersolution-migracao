using Newtonsoft.Json;
using System;

namespace SolutionsTools.Services
{
    public class ReturnServices
    {
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "Href")]
        public string Href { get; set; }
        [JsonProperty(PropertyName = "DocumentId")]
        public Guid DocumentId { get; set; }
        [JsonProperty(PropertyName = "Message")]
        public string Message { get; set; }
    }
}
