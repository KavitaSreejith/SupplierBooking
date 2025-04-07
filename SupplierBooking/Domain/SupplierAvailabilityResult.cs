using Domain;
using NodaTime;

namespace SupplierBooking.Domain
{
    /// <summary>
    /// Represents the result of a supplier availability check
    /// </summary>
    public class SupplierAvailabilityResult
    {
        /// <summary>
        /// Gets the next date when the supplier is available
        /// </summary>
        public LocalDate NextAvailableDate { get; }

        /// <summary>
        /// Gets the collection of public holidays that affected the calculation
        /// </summary>
        public IReadOnlyCollection<PublicHoliday> AffectedHolidays { get; }

        /// <summary>
        /// Gets a value indicating whether the check was performed after the cutoff time
        /// </summary>
        public bool WasAfterCutoff { get; }

        /// <summary>
        /// Gets the cutoff date and time that was used in the calculation
        /// </summary>
        public ZonedDateTime? CutoffDateTime { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SupplierAvailabilityResult"/> class
        /// </summary>
        public SupplierAvailabilityResult(
            LocalDate nextAvailableDate,
            IReadOnlyCollection<PublicHoliday> affectedHolidays,
            bool wasAfterCutoff,
            ZonedDateTime? cutoffDateTime)
        {
            NextAvailableDate = nextAvailableDate;
            AffectedHolidays = affectedHolidays;
            WasAfterCutoff = wasAfterCutoff;
            CutoffDateTime = cutoffDateTime;
        }
    }
}