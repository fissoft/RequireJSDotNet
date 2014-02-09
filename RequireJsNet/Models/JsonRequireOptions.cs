﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RequireJsNet.Models
{
    internal class JsonRequireOptions
    {
        [JsonProperty(PropertyName = "locale")]
        public string Locale { get; set; }
        [JsonProperty(PropertyName = "pageOptions")]
        public dynamic PageOptions { get; set; }
        [JsonProperty(PropertyName = "websiteOptions")]
        public dynamic WebsiteOptions { get; set; }
    }
}
