using System.Text.Json;

namespace Teabot.Agent;

public class ToolRegistry
{
    private readonly Dictionary<string, ITool> tools = [];

    public void Register(ITool tool) => tools[tool.Name] = tool;

    public bool TryInvoke(string name, JsonElement args, out string? result)
    {
        result = null;

        if (!tools.TryGetValue(name, out var tool))
            return false;

        result = tool.Execute(args);
        return true;
    }

    public JsonElement DescribeTools()
    {
        var toolsInfo = tools.Values.Select(t => new
        {
            type = "function",
            function = new
            {
                name = t.Name,
                description = t.Description,
                parameters = new
                {
                    type = "object",
                    properties = t.Parameters,
                    required = t.Required
                }
            }
        });

        var json = JsonSerializer.Serialize(toolsInfo);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

}
