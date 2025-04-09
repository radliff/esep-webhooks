using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    public string FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation($"FunctionHandler received: {input}");

        // Parse API Gateway wrapper
        dynamic wrapper = JsonConvert.DeserializeObject<dynamic>(input.ToString());

        string rawBody = wrapper?.body;
        if (string.IsNullOrEmpty(rawBody))
        {
            context.Logger.LogError("Missing body in event payload.");
            return "Missing body.";
        }

        // Parse the actual GitHub payload
        dynamic json = JsonConvert.DeserializeObject<dynamic>(rawBody);
        string issueUrl = json?.issue?.html_url;

        if (string.IsNullOrEmpty(issueUrl))
        {
            context.Logger.LogError("Missing issue URL in payload.");
            return "Invalid payload structure.";
        }

        string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
        if (string.IsNullOrEmpty(slackUrl))
        {
            context.Logger.LogError("SLACK_URL environment variable is missing!");
            return "Slack URL not configured.";
        }

        string payload = $"{{\"text\":\"Issue Created: {issueUrl}\"}}";

        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, slackUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = client.Send(request);
        var responseContent = response.Content.ReadAsStringAsync().Result;

        return responseContent;
    }
}
