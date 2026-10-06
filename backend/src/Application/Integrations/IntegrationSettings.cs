namespace Worknexa.Application.Integrations;

public sealed class IntegrationSettings
{
    public const string SectionName = "Integrations";

    /// <summary>Runs due syncs in the background. Manual sync always works.</summary>
    public bool BackgroundSync { get; set; } = true;

    public int IntervalMinutes { get; set; } = 15;

    public int PastDays { get; set; } = 30;

    public int FutureDays { get; set; } = 180;

    /// <summary>Incremental cursors older than this are dropped so the sync window moves forward.</summary>
    public int CursorMaxAgeDays { get; set; } = 7;

    public int RunsKept { get; set; } = 50;

    /// <summary>Where the browser lands after connecting. Relative paths resolve against the callback's origin.</summary>
    public string ReturnUrl { get; set; } = "/settings";
}
