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
[FunctionName("MyHttpTrigger")]
public static async Task<IActionResult> Run(
	[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
	ILogger log)
{
	log.LogInformation("C# HTTP trigger function processed a request.");

	//string sourceRefName = "refs/heads/mytopic";



	dynamic data = await req.Content.ReadAsAsync<object>();
    // Get the pull request object from the service hooks payload
	dynamic jObject = JsonConvert.DeserializeObject(data.ToString());
	// Get the pull request id
	int pullRequestId;
	if (!Int32.TryParse(jObject.resource.pullRequestId.ToString(), out pullRequestId))
		return new BadRequestObjectResult("wtf");
    ;



	// Get the pull request title
	string pullRequestTitle = jObject.resource.title;
	string sourceRefName = jObject.resource.sourceRefName;

	if (Regex.Match(sourceRefName, "[A-Z]{1,10}-[0-9]{1,8}").Success) {
		Regex regex = new Regex("[A-Z]{1,10}-[0-9]{1,8}");
		Match match = regex.Match(sourceRefName);
		string ticket = match.Groups[1].Value;
		IssueToTransition(ticket).Wait();

		return new BadRequestObjectResult($"sourceRefName from DevOps test {sourceRefName}\n");
	} else {
		if (Regex.Match(sourceRefName, "mytopic").Success)
			return new BadRequestObjectResult($"sourceRefName from DevOps test {sourceRefName}\n");
		else
			return new BadRequestObjectResult($"Unexpected value {sourceRefName}\n");
	}
}


private static async Task UpdateTransition(string jiraServer, string ticketPath, string jiraToken, string transitionUpdateJson)
{
	//Console.WriteLine(jiraServer + " " + ticketPath + " " + jiraToken + " " + transitionUpdateJson);

	var response = "";

	using (HttpClient client = new HttpClient()) {
		client.DefaultRequestHeaders.Add("Authorization", "Basic " + jiraToken);
		HttpResponseMessage var = await client.PostAsync(jiraServer + ticketPath, new StringContent(transitionUpdateJson, System.Text.Encoding.UTF8, "application/json"));
		response = var.StatusCode.ToString();
	}

	Console.WriteLine("UpdateTransition: attempt results = " + response);
}


static async Task IssueToTransition(string jiraIssue)
{
	string jiraToken = System.Environment.GetEnvironmentVariable("Secret");
	//string jiraToken = "ZGFsZS5laW5hcnNvbkBjYmMuY2E6Z0phVjI4V0ZwWktEOHFrS2hKalNCMTM2";
	string jiraServer = "https://cbcradiocanada.atlassian.net";
	string ticketPath = "/rest/api/2/issue/" + jiraIssue + "/transitions";
	string url = jiraServer + ticketPath;
    return new BadRequestObjectResult($"Unexpected value {jiraIssue}\n");
	string codeReviewId = await GetReviewId(url, jiraToken);
	//string codeReviewId = ""; //  I want ^ to provide this

	string transitionUpdateJson = "{ \"update\": {  \"comment\": [  {  \"add\": {  \"body\": \"Comment added when resolving issue\"  }  }  ]  },  \"transition\": {  \"id\": \"" + codeReviewId + "\"  } }";

	// await UpdateTransition(jiraServer, ticketPath, jiraToken, transitionUpdateJson);
}



static async Task<string> GetReviewId(string url, string jiraToken)
{
	string jiraTransitions = "";

	using (HttpClient client = new HttpClient()) {
		client.DefaultRequestHeaders.Accept.Clear();
		client.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json"));
		client.DefaultRequestHeaders.Add("Authorization", "Basic ZGFsZS5laW5hcnNvbkBjYmMuY2E6Z0phVjI4V0ZwWktEOHFrS2hKalNCMTM2");
		jiraTransitions = await client.GetStringAsync(url);
	}

	JObject transitionsJson = JObject.Parse(jiraTransitions);
	var codeReviewId = transitionsJson["transitions"].Where(a => (string)a["name"] == "Code Review")
			   .Select(a => (string)a["id"])
			   .FirstOrDefault();

	if (string.IsNullOrEmpty(codeReviewId)) {
		// create an error
		Console.WriteLine("GetReviewId: 'Code Review' not in ticket " + jiraTransitions);
		return "null";
	} else {
		// Id found
		Console.WriteLine("GetReviewId: 'Code Review' found " + codeReviewId);
		return codeReviewId;
	}
}
}
}
