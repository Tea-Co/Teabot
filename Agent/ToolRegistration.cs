namespace Teabot.Agent;

public class ToolRegistration
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public string[] Required { get; set; }
    public string Endpoint { get; set; }

}
