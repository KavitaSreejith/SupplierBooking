using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NodaTime;
using SupplierBooking.Domain.Interfaces;

namespace SupplierBooking.Infrastructure.Services
{
    /// <summary>
    /// Implementation of business day calculator with caching
    /// </summary>
    public class BusinessDayCalculator : IBusinessDayCalculator
    {
        private readonly IPublicHolidayProvider _holidayProvider;
        private readonly ILogger<BusinessDayCalculator> _logger;
        private readonly IMemoryCache _cache;

        private static readonly HashSet<IsoDayOfWeek> _weekendDays = new HashSet<IsoDayOfWeek>
        {
            IsoDayOfWeek.Saturday,
            IsoDayOfWeek.Sunday
        };

        private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessDayCalculator"/> class
        /// </summary>
        public BusinessDayCalculator(
            IPublicHolidayProvider holidayProvider,
            IMemoryCache cache,
            ILogger<BusinessDayCalculator> logger)
        {
            _holidayProvider = holidayProvider ?? throw new ArgumentNullException(nameof(holidayProvider));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<LocalDate> GetPreviousBusinessDayAsync(
            LocalDate date,
            string state,
            CancellationToken cancellationToken = default)
        {
            var previousDay = date.PlusDays(-1);

            while (!await IsBusinessDayAsync(previousDay, state, cancellationToken))
            {
                previousDay = previousDay.PlusDays(-1);
            }

            return previousDay;
        }

        /// <inheritdoc/>
        public async Task<LocalDate> GetNextBusinessDayAsync(
            LocalDate date,
            string state,
            CancellationToken cancellationToken = default)
        {
            var nextDay = date.PlusDays(1);

            while (!await IsBusinessDayAsync(nextDay, state, cancellationToken))
            {
                nextDay = nextDay.PlusDays(1);
            }

            return nextDay;
        }

        /// <inheritdoc/>
        public async Task<bool> IsBusinessDayAsync(
            LocalDate date,
            string state,
            CancellationToken cancellationToken = default)
        {
            // First, check if it's a weekend
            if (_weekendDays.Contains(date.DayOfWeek))
            {
                return false;
            }

            // Check if it's a public holiday
            // Use caching to avoid repeated calls to the holiday provider
            var cacheKey = $"IsHoliday_{state}_{date:yyyy-MM-dd}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SetAbsoluteExpiration(_cacheDuration);

                // Get holidays for the specific date
                var holidays = await _holidayProvider.GetHolidaysAsync(
                    state, date, date, cancellationToken);

                // If there are no holidays on this date, it's a business day
                var isBusinessDay = !holidays.Any();

                _logger.LogDebug("Date {Date} in state {State} is{NotBusinessDay} a business day",
                    date, state, isBusinessDay ? "" : " not");

                return isBusinessDay;
            });
        }
    }
}