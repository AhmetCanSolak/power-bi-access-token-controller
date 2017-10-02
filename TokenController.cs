using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Rest;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Web.Mvc;
using System.Web.Http.Cors;


namespace BackendProject.WebApi.Controllers
{
    public class EmbedConfig
    {
        public string Id { get; set; }

        public string EmbedUrl { get; set; }

        public string EmbedToken { get; set; }

        public string ErrorMessage { get; internal set; }
    }

    [EnableCors(origins: "IP TO ALLOW", headers: "*", methods: "*")]
    public class TokenController : ApiController
    {


        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.ActionName("getToken")]
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> LoginAsync()
        {
            var response = await EmbedReport();

            return Ok(response);
        }


        public async Task<EmbedConfig> EmbedReport()
        {

            var Username = "USERNAME";
            var Password = "PASSWORD";
            // Create a user password cradentials.
            var credential = new UserPasswordCredential(Username, Password);

            //Authenticate On Azure. 
            var AuthorityUrl = "https://login.windows.net/common/oauth2/authorize/";
            var ResourceUrl = "https://analysis.windows.net/powerbi/api";
            var ClientId = "YOUR CLIENT ID HERE";
            var authenticationContext = new AuthenticationContext(AuthorityUrl);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(ResourceUrl, ClientId, credential);

            //GetToken If Authenticated
            if (authenticationResult == null)
            {
                return null;
            }
            var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");


            var ApiUrl = "https://api.powerbi.com/";
            using (var client = new PowerBIClient(new Uri(ApiUrl), tokenCredentials))
            {
                // Get a list of reports. 
                var GroupId = "GROUP ID";
                var reports = await client.Reports.GetReportsInGroupAsync(GroupId);

                // Get the related report in the group.
                var ReportId = "REPORT ID";
                var report = reports.Value.Where(k => k.Id == ReportId).FirstOrDefault();

                if (report == null)
                {
                    return null;
                }

                // Generate Embed Token.
                var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(GroupId, report.Id, generateTokenRequestParameters);

                if (tokenResponse == null)
                {
                    return null;
                }

                var Tokken = tokenResponse.Token;
                // Generate Embed Configuration. These properties (EmbedToken, EmbedUrl, Id) could be used on fronthand to embed powerbi report
                var embedConfig = new EmbedConfig()
                {
                    EmbedToken = tokenResponse.Token,
                    EmbedUrl = report.EmbedUrl,
                    Id = report.Id
                };

                return embedConfig;

            }


        }
    }
}
