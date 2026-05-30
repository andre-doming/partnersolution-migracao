using Newtonsoft.Json;

namespace SolutionsTools.Services
{
    public class ReturnUser
    {
        [JsonProperty(PropertyName = "status")]
        public bool status { get; set; }

        [JsonProperty(PropertyName = "ret_code")]
        public int code { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string message { get; set; }
    }

}
