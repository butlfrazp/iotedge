// Copyright (c) Microsoft. All rights reserved.
namespace DevOpsLib
{
    using System;
    using System.Threading.Tasks;
    using DevOpsLib.VstsModels;
    using Flurl;
    using Flurl.Http;
    using Newtonsoft.Json.Linq;

    public class BugManagement
    {
        const string WorkItemPathSegmentFormat = "{0}/{1}/{2}/_apis/wit/workitems/$Bug";

        readonly DevOpsAccessSetting accessSetting;

        public BugManagement(DevOpsAccessSetting accessSetting)
        {
            this.accessSetting = accessSetting;
        }

        /// <summary>
        /// This method is used to create a bug in Azure Dev Ops.
        /// If it cannot create the bug it will rethrow the exception from the DevOps api.
        /// Reference: https://docs.microsoft.com/en-us/rest/api/azure/devops/wit/work%20items/create?view=azure-devops-rest-6.0
        /// </summary>
        /// <param name="branch">Branch for which the bug is being created</param>
        /// <param name="build">Build for which the bug is being created</param>
        /// <returns>Work item id for the created bug.</returns>
        public async Task<string> CreateBugAsync(string branch, VstsBuild build)
        {
            string requestPath = string.Format(WorkItemPathSegmentFormat, DevOpsAccessSetting.BaseUrl, this.accessSetting.Organization, this.accessSetting.Project);
            IFlurlRequest workItemQueryRequest = ((Url)requestPath)
                .WithBasicAuth(string.Empty, this.accessSetting.PersonalAccessToken)
                .WithHeader("Content-Type", "application/json-patch+json")
                .SetQueryParam("api-version", "6.0");

            var jsonBody = new object[]
            {
                new {
                    op = "add",
                    path = "/fields/System.Title",
                    from = string.Empty,
                    value = $"Test failure on {branch}: {build.DefinitionId.ToString()} {build.BuildNumber}"
                },
                new
                {
                    op = "add",
                    path = "/fields/Microsoft.VSTS.TCM.ReproSteps",
                    from = string.Empty,
                    value = $"This bug is autogenerated by the vsts-pipeline-sync service. Link to failing build:<div> {build.WebUri}"
                },
                new
                {
                    op = "add",
                    path = "/fields/Microsoft.VSTS.Common.Priority",
                    from = string.Empty,
                    value = "0"
                },
                new
                {
                    op = "add",
                    path = "/fields/System.AreaPath",
                    from = string.Empty,
                    value = "One\\IoT\\Platform and Devices\\IoTEdge"
                },
                new
                {
                    op = "add",
                    path = "/relations/-",
                    value = new
                    {
                        rel = "Hyperlink",
                        url = $"{build.WebUri}"
                    }
                }
            };

            JObject result;
            try
            {
                IFlurlResponse response = await workItemQueryRequest
                    .PostJsonAsync(jsonBody);

                result = await response.GetJsonAsync<JObject>();
            }
            catch (FlurlHttpException e)
            {
                string message = $"Failed making call to vsts work item api: {e.Message}";
                Console.WriteLine(message);
                Console.WriteLine(e.Call.RequestBody);
                Console.WriteLine(e.Call.Response.StatusCode);
                Console.WriteLine(e.Call.Response.ResponseMessage);

                throw new Exception(message);
            }

            return result["id"].ToString();
        }
    }
}
