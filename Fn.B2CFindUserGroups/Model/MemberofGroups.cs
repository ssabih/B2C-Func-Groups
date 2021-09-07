using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fn.B2CFindUserGroups.Model
{
    public class Groups
    {
        [JsonProperty("@odata.context")]
        public string OdataContext { get; set; }
        public List<string> value { get; set; }
    }
}
