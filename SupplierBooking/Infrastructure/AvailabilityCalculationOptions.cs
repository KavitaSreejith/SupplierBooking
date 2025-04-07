using NodaTime;

/// <summary>
/// Configuration options for availability calculation
/// </summary>
public class AvailabilityCalculationOptions
{
    /// <summary>
    /// Gets or sets the time zone ID to use for cutoff calculations
    /// </summary>
    // IMPORTANT: This must be set to Australia/Sydney for AEST timezone
    public string TimeZoneId { get; set; } = "Australia/Sydney";

    /// <summary>
    /// Gets or sets the cutoff time of day
    /// </summary>
    public LocalTime CutoffTime { get; set; } = new LocalTime(12, 0, 0);

    /// <summary>
    /// Gets or sets the number of business days before a holiday to check for cutoff
    /// </summary>
    public int BusinessDaysBeforeHoliday { get; set; } = 2;

    /// <summary>
    /// Gets or sets the number of days to look ahead for holidays
    /// </summary>
    public int HolidayLookAheadDays { get; set; } = 60;
}