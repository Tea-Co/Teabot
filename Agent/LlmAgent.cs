using System.Text;
using System.Text.Json;

namespace Teabot.Agent;

public class LlmAgent(ToolRegistry registry)
{
    private const string url = "http://host.docker.internal:11434/v1/chat/completions";
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromDays(1) };
    private readonly List<dynamic> messages = [];
    private readonly AgentState state = new();
    private int? checklistMessageIndex = null;
    private bool reflecting = true;

    public async Task RunAsync(string prompt)
    {
        messages.Clear();
        messages.Add(new
        {
            role = "user",
            content =
            $"""
            You are a precise, autonomous AI Agent. Begin by breaking the user's task into subtasks. Here is your goal:

            {prompt}
            
            Please respond only with a checklist of steps like:
            - [ ] Step 1
            - [ ] Step 2
            /no_think
            """
        });

        var checklist = await GenerateChecklistAsync();
        state.SetChecklist(checklist);

        messages.Clear();
        messages.Add(new
        {
            role = "user",
            content = prompt + "/think"
        });

        while (!state.AllTasksComplete())
        {
            // Console.WriteLine($"[📋 Checklist:\n{state.RenderChecklist()}]");

            if (checklistMessageIndex is int idx && idx < messages.Count)
                messages.RemoveAt(idx);

            var checklistMessage = reflecting ?
            $"""
            You are a fully autonomous, self-directed AI Agent.
            - Never ask the user questions.
            - Always decide and act on your own.
            - If faced with options, choose the one that best completes your current checklist.

            Checklist:
            {state.RenderChecklist()}

            You shall now review your progress and update the checklist:
            - Mark [x] for completed items based on what you've done
            - Leave [ ] for tasks you haven't finished
            - If something was skipped, explain briefly.

            Then decide which step to do next, but **do not begin it yet**.
            Just say: "**Next task: Step N - [name]**"
            """ :
            """
            You are a fully autonomous, self-directed AI Agent.
            - Never ask the user questions.
            - Always decide and act on your own.
            - If faced with options, choose the one that best completes your current checklist.

            You've already decided the next task.

            Now execute it decisively and completely.
            """;

            messages.Add(new
            {
                role = "user",
                content = checklistMessage
                
            });
            checklistMessageIndex = messages.Count - 1;

            var requestPayload = new
            {
                model = "qwen3:30b-a3b",
                messages,
                tools = registry.DescribeTools(),
                tool_choice = "auto",
                think = false,
            };

            var response = await client.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json")
            );

            var parsed = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            await ProcessResponseAsync(parsed);

            reflecting = !reflecting;
        }
    }

    private async Task<List<string>> GenerateChecklistAsync()
    {
        var response = await client.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(new {
                model = "qwen3:30b-a3b",
                messages,
            }), Encoding.UTF8, "application/json")
        );

        var text = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString() ?? "";

        Console.WriteLine("[📋 Checklist Generated]");
        Console.WriteLine(text);

        var lines = text.Split('\n').Where(l => l.Trim().StartsWith("- [")).ToList();
        return lines;
    }

    private async Task ProcessResponseAsync(JsonDocument parsed)
    {
        var choice = parsed.RootElement.GetProperty("choices")[0];
        var message = choice.GetProperty("message");

        ProcessContent(message);
        ProcessTools(message);
    }

    private void ProcessContent(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content))
            return;

        var parts = content.GetString().Split(["<think>\n", "\n</think>\n\n"], StringSplitOptions.None);

        if (parts.Length > 1) // think
            Console.WriteLine($"[🧠 {parts[1]}]");

        if (parts.Length > 2) // main response
            Console.WriteLine(parts[2]);

        messages.Add(new { role = "assistant", content });
        state.TryUpdateChecklist(content.GetString() ?? "");
    }

    private void ProcessTools(JsonElement message)
    {
        if (!message.TryGetProperty("tool_calls", out var functionCall))
            return;

        foreach (var call in functionCall.EnumerateArray())
        {
            var function = call.GetProperty("function");
            var name = function.GetProperty("name").GetString() ?? string.Empty;
            var rawArgs = function.GetProperty("arguments").GetString() ?? "{}";

            var args = JsonDocument.Parse(rawArgs).RootElement;

            registry.TryInvoke(name, args, out var toolResult);

            var toolMessage = $"[Tool: {name}] → {toolResult}";
            Console.WriteLine(toolMessage);

            messages.Add(new { role = "tool", content = toolMessage });
        }
    }

}
