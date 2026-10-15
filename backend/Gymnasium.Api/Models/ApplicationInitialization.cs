namespace Gymnasium.Api.Models;

/// <summary>Records bootstrap tasks so a redeploy never restores deleted content.</summary>
public sealed class ApplicationInitialization
{
    public required string Name { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
