using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace SolutionsTools.WebApi
{
    [Serializable]
    [JsonObject("ret_generic")]
    public class ReturnGeneric
    {

        [JsonProperty(PropertyName = "ret_code")]
        public int ret_code { get; set; }

        [JsonProperty(PropertyName = "ret_info")]
        public string ret_info { get; set; }
    }

}
