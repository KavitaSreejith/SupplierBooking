using NodaTime;

namespace Domain
{
    /// <summary>
    /// Represents a public holiday
    /// </summary>
    public class PublicHoliday
    {
        /// <summary>
        /// Gets the date of the holiday
        /// </summary>
        public LocalDate Date { get; }

        /// <summary>
        /// Gets the name of the holiday
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the state(s) in which this holiday is observed
        /// </summary>
        public IReadOnlyCollection<string> States { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicHoliday"/> class
        /// </summary>
        public PublicHoliday(LocalDate date, string name, IReadOnlyCollection<string> states)
        {
            Date = date;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            States = states ?? throw new ArgumentNullException(nameof(states));
        }
    }
}
