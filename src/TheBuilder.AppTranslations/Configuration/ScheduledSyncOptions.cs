namespace TheBuilder.AppTranslations.Configuration;

/// <summary>
/// Syncing every enabled source on a schedule. It is off until <see cref="Interval"/> is set.
/// </summary>
public sealed class ScheduledSyncOptions
{
    public const string SectionName = "AppTranslations:ScheduledSync";
    public const string IntervalConfigurationName = $"{SectionName}:Interval";

    /// <summary>The shortest interval, which is also how often the schedule is checked.</summary>
    internal static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long after a source's last sync it syncs again, counting syncs started from the
    /// backoffice and from a deployment. <c>1.00:00:00</c> is once a day.
    /// </summary>
    public TimeSpan? Interval { get; set; }
}
