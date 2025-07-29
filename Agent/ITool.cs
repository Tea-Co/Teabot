using System.Text.Json;

namespace Teabot.Agent;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    Dictionary<string, object> Parameters { get; }
    string[] Required { get; }

    string Execute(JsonElement args);

}
