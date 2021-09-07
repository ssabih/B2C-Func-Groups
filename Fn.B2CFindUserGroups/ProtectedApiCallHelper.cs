// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Fn.B2CFindUserGroups
{

    public class ProtectedApiCallHelper
    {
        public ProtectedApiCallHelper(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }
        protected HttpClient HttpClient { get; private set; }

        public async Task<JObject> CallWebApiAndProcessResultASync(string webApiUrl, string accessToken, ILogger log)
        {
            JObject result = null;
            if (!string.IsNullOrEmpty(accessToken))
            {
                var defaultRequestHeaders = HttpClient.DefaultRequestHeaders;
                if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
                {
                    HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                }
                defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                JObject httpContentBody= new JObject(new JProperty("securityEnabledOnly", "true"));
                var httpContent = new StringContent(httpContentBody.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await HttpClient.PostAsync(webApiUrl, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject(json) as JObject;                    
                }
                else
                {
                    log.LogInformation($"Failed to call Graph-API: {response.StatusCode}");
                    string content = await response.Content.ReadAsStringAsync();
                    result = (JObject)content;
                    log.LogInformation($"Content: {content}");
                }                
            }
            return result;
        }
    }
}
