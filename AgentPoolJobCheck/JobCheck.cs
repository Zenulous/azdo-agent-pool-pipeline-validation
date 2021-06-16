using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using YamlDotNet.Serialization;
using System.Collections.Generic;
using System.Linq;
namespace AgentPoolJobCheck
{
    public static class JobCheck
    {
        [FunctionName("JobCheck")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            RequestBody data = JsonConvert.DeserializeObject<RequestBody>(requestBody);
            Uri collectionUri = data.PlanUrl;
            string pat = data.AuthToken;
            var connection = new VssConnection(collectionUri, new VssBasicCredential("", pat));
            var buildClient = connection.GetClient<BuildHttpClient>();
            var timeline = await buildClient.GetBuildLogAsync(data.ProjectId, data.BuildId, 1);
            var distributedTaskClient = connection.GetClient<TaskAgentHttpClient>();
            Console.WriteLine(timeline);
            StreamReader reader = new StreamReader(timeline);
            string ymlPipelineToRun = reader.ReadToEnd();
            var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
            var pipeline123 = deserializer.Deserialize<dynamic>(ymlPipelineToRun);
            var pipeline = deserializer.Deserialize<Pipeline>(ymlPipelineToRun);
            var stages = pipeline.stages;
            var allowedTasks = new string[] { "NuGetToolInstaller@1" };
            foreach (var stage in pipeline.stages)
            {
                foreach (var job in stage.jobs)
                {
                    foreach (var step in job.steps)
                    {
                        System.Console.WriteLine(step.task);
                        if (!(allowedTasks.Contains(step.task)))
                        {
                            return new BadRequestObjectResult("Illegal task detected");
                        }


                    }
                }

            }
            return new OkObjectResult("");
        }
    }
}

public partial class Pipeline
{
    // This class only contains a subset of all the properties of a pipeline
    // We only case about the tasks in steps
    public List<Stage> stages { get; set; }
}

public partial class Stage
{
    public List<Job> jobs { get; set; }
}

public partial class Job
{
    public List<Step> steps { get; set; }
}
public partial class Step
{
    public string task { get; set; }
}
public class RequestBody
{
    [JsonProperty("PlanUrl")]
    public Uri PlanUrl { get; set; }

    [JsonProperty("AuthToken")]
    public string AuthToken { get; set; }

    [JsonProperty("BuildId")]
    public int BuildId { get; set; }

    [JsonProperty("ProjectId")]
    public string ProjectId { get; set; }
}
