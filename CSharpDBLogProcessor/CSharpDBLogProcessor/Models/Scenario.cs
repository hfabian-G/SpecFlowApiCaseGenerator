using System.Text.Json.Nodes;

public class Scenario
{
    public required string ApplicationId { get; set; }
    public required string Route { get; set; }
    public required JsonObject Message { get; set; }
    public JsonObject? InputParameters { get; set; }
    public JsonObject? Response { get; set; }
    public string? Authorization { get; set; }
    public string? ScenarioName;
    public string? ScenarioOutput;
}