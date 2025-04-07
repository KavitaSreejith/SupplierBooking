using NodaTime;
using SupplierBooking.Domain.Interfaces;

namespace SupplierBooking.Infrastructure.Services
{
    /// <summary>
    /// Provider for current date and time
    /// </summary>
    public class ClockProvider : IClockProvider
    {
        private readonly IClock _clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClockProvider"/> class
        /// </summary>
        /// <param name="clock">The clock to use. Defaults to system clock.</param>
        public ClockProvider(IClock? clock = null)
        {
            _clock = clock ?? SystemClock.Instance;
        }

        /// <inheritdoc/>
        public ZonedDateTime GetCurrentDateTime(string timeZoneId = "Australia/Sydney")
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                throw new ArgumentException("Time zone ID must be provided", nameof(timeZoneId));
            }

            var timeZone = DateTimeZoneProviders.Tzdb[timeZoneId];
            return _clock.GetCurrentInstant().InZone(timeZone);
        }
    }
}