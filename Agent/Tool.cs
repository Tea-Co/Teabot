using System.Text;
using System.Text.Json;

namespace Teabot.Agent;

public class Tool(
    string name,
    string description,
    Dictionary<string, object> parameters,
    string[] required,
    string endpoint
) : ITool
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public Dictionary<string, object> Parameters { get; } = parameters;
    public string[] Required { get; } = required;

    private readonly HttpClient client = new();

    public string Execute(JsonElement args)
    {
        var content = new StringContent(args.GetRawText(), Encoding.UTF8, "application/json");
        var response = client.PostAsync(endpoint, content).Result;

        return response.IsSuccessStatusCode
            ? response.Content.ReadAsStringAsync().Result
            : $"Tool execution failed: {response.StatusCode}";
    }

}
