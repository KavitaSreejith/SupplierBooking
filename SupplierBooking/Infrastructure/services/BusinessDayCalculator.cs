using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NodaTime;
using SupplierBooking.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SupplierBooking.Infrastructure.Services
{
    /// <summary>
    /// Optimized implementation of business day calculator with improved caching and performance
    /// </summary>
    public sealed class BusinessDayCalculator : IBusinessDayCalculator
    {
        private readonly IPublicHolidayProvider _holidayProvider;
        private readonly ILogger<BusinessDayCalculator> _logger;
        private readonly IMemoryCache _cache;

        // Use a static readonly field for performance - this never changes
        private static readonly HashSet<IsoDayOfWeek> _weekendDays = new()
        {
            IsoDayOfWeek.Saturday,
            IsoDayOfWeek.Sunday
        };

        // Cache keys and expiration
        private const string CacheKeyPrefix = "BusinessDay_v1_";
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(8); // Increased from 1h to 8h

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizedBusinessDayCalculator"/> class
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
            // Check cache for previous business day
            string cacheKey = $"{CacheKeyPrefix}Prev_{state}_{date:yyyy-MM-dd}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SetAbsoluteExpiration(_cacheDuration);

                var previousDay = date.PlusDays(-1);

                while (!await IsBusinessDayInternalAsync(previousDay, state, cancellationToken).ConfigureAwait(false))
                {
                    previousDay = previousDay.PlusDays(-1);
                }

                return previousDay;
            });
        }

        /// <inheritdoc/>
        public async Task<LocalDate> GetNextBusinessDayAsync(
            LocalDate date,
            string state,
            CancellationToken cancellationToken = default)
        {
            // Check cache for next business day
            string cacheKey = $"{CacheKeyPrefix}Next_{state}_{date:yyyy-MM-dd}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SetAbsoluteExpiration(_cacheDuration);

                var nextDay = date.PlusDays(1);

                while (!await IsBusinessDayInternalAsync(nextDay, state, cancellationToken).ConfigureAwait(false))
                {
                    nextDay = nextDay.PlusDays(1);
                }

                return nextDay;
            });
        }

        /// <inheritdoc/>
        public Task<bool> IsBusinessDayAsync(
            LocalDate date,
            string state,
            CancellationToken cancellationToken = default)
        {
            return IsBusinessDayInternalAsync(date, state, cancellationToken);
        }

        /// <summary>
        /// Internal implementation that enables code reuse while maintaining cache separation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<bool> IsBusinessDayInternalAsync(
            LocalDate date,
            string state,
            CancellationToken cancellationToken)
        {
            // Quick check for weekend - no need to hit cache or database
            if (_weekendDays.Contains(date.DayOfWeek))
            {
                return false;
            }

            // Check if it's a public holiday - use caching
            var cacheKey = $"{CacheKeyPrefix}IsHoliday_{state}_{date:yyyy-MM-dd}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SetAbsoluteExpiration(_cacheDuration);
                entry.SetPriority(CacheItemPriority.High); // Prioritize this cache item

                // Get holidays for the specific date
                var holidays = await _holidayProvider.GetHolidaysAsync(
                    state, date, date, cancellationToken).ConfigureAwait(false);

                // If there are no holidays on this date, it's a business day
                var isBusinessDay = holidays.Count == 0;

                return isBusinessDay;
            });
        }
    }
}