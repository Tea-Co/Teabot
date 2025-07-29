using System.Text.RegularExpressions;

namespace Teabot.Agent;

public class AgentState
{
    private readonly List<(string task, bool complete)> checklist = [];

    public void SetChecklist(List<string> lines)
    {
        checklist.Clear();
        foreach (var line in lines)
        {
            var done = line.Contains("[x]");
            var clean = Regex.Replace(line, @"- \[[ xX]\]\s*", "").Trim();
            checklist.Add((clean, done));
        }
    }

    public void TryUpdateChecklist(string content)
    {
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"- \[( |x|X)\] (.+)");

            if (!match.Success)
                continue;

            var isChecked = match.Groups[1].Value.ToLower() == "x";
            var lineText = match.Groups[2].Value.Trim();

            foreach (var (task, done) in checklist.ToList())
            {
                if (IsSameTask(task, lineText) && isChecked && !done)
                {
                    UpdateTask(task, true);
                }
            }
        }
    }

    public void UpdateTask(string task, bool complete)
    {
        for (int i = 0; i < checklist.Count; i++)
        {
            if (checklist[i].task.Equals(task, StringComparison.OrdinalIgnoreCase))
                checklist[i] = (task, complete);
        }
    }

    public string RenderChecklist() =>
        string.Join("\n", checklist.Select(t => $"- [{(t.complete ? "x" : " ")}] {t.task}"));

    public bool AllTasksComplete() =>
        checklist.All(t => t.complete);

    private static bool IsSameTask(string a, string b) =>
        Normalize(a).Equals(Normalize(b), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string text) =>
        Regex.Replace(text, @"\s+", " ").Trim();

}
