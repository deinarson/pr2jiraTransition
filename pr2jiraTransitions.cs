using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace func_test
{
public static class MyHttpTrigger
{
[FunctionName("c72a634c-7f41-c40f-1094-87a090d84b5d")]
public static async Task<IActionResult> Run(
	[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
	ILogger log)
{
	string jiraToken = System.Environment.GetEnvironmentVariable("Secret");

	log.LogInformation("APP : C# HTTP trigger function processed a request.");

	log.LogInformation($"DEBUGGING remove this line before this hits prod {jiraToken}");
	dynamic data = await req.Content.ReadAsAsync<object>();
	// Get the pull request object from the service hooks payload
	dynamic jObject = JsonConvert.DeserializeObject(data.ToString());
	// Get the pull request id
	int pullRequestId;
	if (!Int32.TryParse(jObject.resource.pullRequestId.ToString(), out pullRequestId))
		return new BadRequestObjectResult("APP: payload not json?");

	// Get the pull request title
	string pullRequestTitle = jObject.resource.title;
	string sourceRefName = jObject.resource.sourceRefName;

	if (Regex.Match(sourceRefName, "[A-Z]{1,10}-[0-9]{1,8}").Success) {
		Regex regex = new Regex("[A-Z]{1,10}-[0-9]{1,8}");
		Match match = regex.Match(sourceRefName);
		string jiraIssue = match.Groups[0].Value;

		log.LogInformation($"APP IssueToTransition: ticket id {jiraIssue}");
		await IssueToTransition(jiraIssue, jiraToken, log);

		log.LogInformation($"APP: sourceRefName from DevOps test {sourceRefName}\n");
		return (ActionResult) new OkObjectResult($"APP: sourceRefName from DevOps test {sourceRefName}\n");
	} else {
		if (Regex.Match(sourceRefName, "mytopic").Success) {
			log.LogInformation($"APP: sourceRefName from Azure DevOps test : {sourceRefName}\n");
			return (ActionResult) new OkObjectResult($"APP: sourceRefName from Azure DevOps test : {sourceRefName}\n");
		} else {
			log.LogInformation($"APP: Unexpected value {sourceRefName}\n");
			return new BadRequestObjectResult($"APP: Unexpected value {sourceRefName}\n");
		}
	}
}


static async Task IssueToTransition(string jiraIssue, string jiraToken, ILogger log)
{
	string jiraServer = "https://cbcradiocanada.atlassian.net";
	string ticketPath = "/rest/api/2/issue/" + jiraIssue + "/transitions";
	string url = jiraServer + ticketPath;

	log.LogInformation($"APP IssueToTransition: url={url}");
	string codeReviewId = await GetReviewId(url, jiraToken, log);
	//string codeReviewId = ""; //  I want ^ to provide this

	string transitionUpdateJson = "{ \"update\": {  \"comment\": [  {  \"add\": {  \"body\": \"Comment added when resolving issue\"  }  }  ]  },  \"transition\": {  \"id\": \"" + codeReviewId + "\"  } }";

	log.LogInformation($"APP IssueToTransition: {jiraServer}, {ticketPath}, {transitionUpdateJson}");
	await UpdateTransition(jiraServer, ticketPath, jiraToken, transitionUpdateJson, log);
}


static async Task<string> GetReviewId(string url, string jiraToken, ILogger log)
{
	string jiraTransitions = "";

	using (HttpClient client = new HttpClient()) {
		client.DefaultRequestHeaders.Accept.Clear();
		client.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json"));
		client.DefaultRequestHeaders.Add("Authorization", "Basic " + jiraToken);
		jiraTransitions = await client.GetStringAsync(url);
	}

	JObject transitionsJson = JObject.Parse(jiraTransitions);
	var codeReviewId = transitionsJson["transitions"].Where(a => (string)a["name"] == "Code Review")
			   .Select(a => (string)a["id"])
			   .FirstOrDefault();

	if (string.IsNullOrEmpty(codeReviewId)) {
		// create an error
		Console.WriteLine("APP GetReviewId: 'Code Review' not in ticket, this is ok \ndebugging details:\n\t" + jiraTransitions);
		return "null";
	} else {
		// Id found
		Console.WriteLine("APP GetReviewId: 'Code Review' found " + codeReviewId);
		return codeReviewId;
	}
}



private static async Task UpdateTransition(string jiraServer, string ticketPath, string jiraToken, string transitionUpdateJson, ILogger log)
{
	//Console.WriteLine(jiraServer + " " + ticketPath + " " + jiraToken + " " + transitionUpdateJson);

	var response = "";

	using (HttpClient client = new HttpClient()) {
		client.DefaultRequestHeaders.Add("Authorization", "Basic " + jiraToken);
		HttpResponseMessage var = await client.PostAsync(jiraServer + ticketPath, new StringContent(transitionUpdateJson, System.Text.Encoding.UTF8, "application/json"));
		response = var.StatusCode.ToString();
	}

	log.LogInformation("APP UpdateTransition: attempt results = " + response);
}
}
}
