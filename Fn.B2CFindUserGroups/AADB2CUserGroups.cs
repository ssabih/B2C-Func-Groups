using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Fn.B2CFindUserGroups.Model;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;


namespace Fn.B2CFindUserGroups
{
    public class ResponseContent
    {
        public string[] groups { get; set; }
    }
    public static class AADB2CUserGroups
    {
        [FunctionName("FnB2CFindUserGroups")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string objectId =  data.objectId;           

            string groups = string.Empty;

            try
            {
                Groups _graphresponse = await GetUserGroups(objectId, log);
                if (_graphresponse != null)
                {
                    string[] responseMessage = _graphresponse.value.ToArray();
                    return (ActionResult)new OkObjectResult(new ResponseContent() { groups = responseMessage });
                }
 
            }
            catch (Exception ex)
            {
                log.LogInformation($" Error: {ex.Message}");
            }
            return new OkObjectResult (groups.ToString());
        }
        private static async Task<Groups> GetUserGroups(string _aaduserobject, ILogger log)
        {
            try
            {

                AuthenticationConfig config = new AuthenticationConfig();
                IConfidentialClientApplication app;

                app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(new Uri(config.Authority))
                    .Build();

                string[] scopes = new string[] { $"{config.ApiUrl}.default" };

                JObject _destinationUseresult = null;

                AuthenticationResult result = null;
                try
                {
                    result = await app.AcquireTokenForClient(scopes)
                        .ExecuteAsync();
                }
                catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
                {
                    log.LogInformation("Scope provided is not supported for destination AAD instance");
                }

                Groups _dest_graphapiuserresponse = null;

                if (result != null)
                {
                    var httpClient = new HttpClient();
                    var apiCaller = new ProtectedApiCallHelper(httpClient);

                    _destinationUseresult = await apiCaller.CallWebApiAndProcessResultASync(
                                                               $"{config.ApiUrl}v1.0/users/{_aaduserobject}/getMemberGroups",
                                                               result.AccessToken,
                                                               log);
                    _dest_graphapiuserresponse = JsonConvert.DeserializeObject<Groups>(_destinationUseresult.ToString());
                }
                return _dest_graphapiuserresponse;
            }
            catch (Exception ex)
            {
                log.LogError(ex,ex.Message);
                throw ex;
            }
        }

    }
}