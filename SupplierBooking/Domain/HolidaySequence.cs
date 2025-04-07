using NodaTime;

namespace Domain;

/// <summary>
/// Represents a sequence of related holidays (like Easter)
/// </summary>
public class HolidaySequence
{
    /// <summary>
    /// Gets the collection of holidays in this sequence
    /// </summary>
    public IReadOnlyList<PublicHoliday> Holidays { get; }

    /// <summary>
    /// Gets the first date in the holiday sequence
    /// </summary>
    public LocalDate FirstDate => Holidays.Min(h => h.Date);

    /// <summary>
    /// Gets the last date in the holiday sequence
    /// </summary>
    public LocalDate LastDate => Holidays.Max(h => h.Date);

    /// <summary>
    /// Gets the name of the holiday sequence
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HolidaySequence"/> class
    /// </summary>
    public HolidaySequence(string name, IReadOnlyList<PublicHoliday> holidays)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Holidays = holidays ?? throw new ArgumentNullException(nameof(holidays));

        if (!holidays.Any())
        {
            throw new ArgumentException("Holiday sequence must contain at least one holiday", nameof(holidays));
        }
    }
}