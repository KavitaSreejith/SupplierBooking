using NodaTime;

namespace SupplierBooking.Domain.Interfaces
{
    /// <summary>
    /// Interface for the clock provider
    /// </summary>
    public interface IClockProvider
    {
        /// <summary>
        /// Gets the current date and time in the specified time zone
        /// </summary>
        /// <param name="timeZoneId">The time zone ID (default: "Australia/Sydney")</param>
        /// <returns>The current date and time in the specified time zone</returns>
        ZonedDateTime GetCurrentDateTime(string timeZoneId = "Australia/Sydney");
    }
}