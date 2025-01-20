using System.Text.Json.Nodes;

public class Log
{
    public required string ApplicationId { get; set; }
    public required string Route { get; set; }
    public required JsonObject Message {get; set; }
}