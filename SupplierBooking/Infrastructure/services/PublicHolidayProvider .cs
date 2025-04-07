using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NodaTime;
using SupplierBooking.Domain;
using SupplierBooking.Domain.Interfaces;
using SupplierBooking.Infrastructure.Data;

namespace SupplierBooking.Infrastructure.Services
{
    /// <summary>
    /// Provider for public holiday data with caching
    /// </summary>
    public class PublicHolidayProvider : IPublicHolidayProvider
    {
        private readonly SupplierBookingContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PublicHolidayProvider> _logger;

        // Increase cache duration for better performance
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

        // Key prefix for cache keys - useful for cache invalidation
        private const string CacheKeyPrefix = "Holiday_v1_";

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicHolidayProvider"/> class
        /// </summary>
        public PublicHolidayProvider(
            SupplierBookingContext dbContext,
            IMemoryCache cache,
            ILogger<PublicHolidayProvider> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<PublicHoliday>> GetHolidaysAsync(
            string state,
            LocalDate startDate,
            LocalDate endDate,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                throw new ArgumentException("State code must be provided", nameof(state));
            }

            if (startDate > endDate)
            {
                throw new ArgumentException("Start date must be before or equal to end date");
            }

            // For single day queries, use a specialized cache key
            bool isSingleDayQuery = startDate == endDate;
            var cacheKey = isSingleDayQuery
                ? $"{CacheKeyPrefix}SingleDay_{state}_{startDate:yyyy-MM-dd}"
                : $"{CacheKeyPrefix}Range_{state}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SetAbsoluteExpiration(_cacheDuration);

                // Log at debug level to reduce noise
                _logger.LogDebug("Fetching holidays for state {State} from {StartDate} to {EndDate}",
                    state, startDate, endDate);

                // Convert NodaTime dates to DateTime for EF Core
                var startDateTime = startDate.ToDateTimeUnspecified();
                var endDateTime = endDate.ToDateTimeUnspecified();

                // Optimize query with eager loading and No-Tracking for read-only data
                var query = _dbContext.PublicHolidays
                    .AsNoTracking()
                    .Include(h => h.States)
                    .Where(h => h.Date >= startDateTime && h.Date <= endDateTime)
                    .Where(h => h.States.Any(s => s.StateCode == state));

                // Add specific optimization for single day queries
                if (isSingleDayQuery)
                {
                    query = query.Where(h => h.Date == startDateTime);
                }

                // Execute query with compiled LINQ for better performance
                var holidays = await query.ToListAsync(cancellationToken);

                // Convert to domain models efficiently using ToList + Select instead of Select + ToList
                var result = holidays.Select(h => new PublicHoliday(
                    LocalDate.FromDateTime(h.Date),
                    h.Name,
                    h.States.Select(s => s.StateCode).ToList()
                )).ToList();

                _logger.LogDebug("Found {HolidayCount} holidays for state {State} in date range",
                    result.Count, state);

                return result as IReadOnlyCollection<PublicHoliday>;
            }) ?? new List<PublicHoliday>();  // Return empty list if cache returns null
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<HolidaySequence>> GetHolidaySequencesAsync(
            string state,
            LocalDate startDate,
            LocalDate endDate,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                throw new ArgumentException("State code must be provided", nameof(state));
            }

            if (startDate > endDate)
            {
                throw new ArgumentException("Start date must be before or equal to end date");
            }

            var cacheKey = $"{CacheKeyPrefix}Sequences_{state}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SetAbsoluteExpiration(_cacheDuration);

                _logger.LogDebug("Fetching holiday sequences for state {State} from {StartDate} to {EndDate}",
                    state, startDate, endDate);

                // Convert NodaTime dates to DateTime for EF Core
                var startDateTime = startDate.ToDateTimeUnspecified();
                var endDateTime = endDate.ToDateTimeUnspecified();

                // Optimize query with batch loading and No-Tracking for read-only data
                var sequences = await _dbContext.HolidaySequences
                    .AsNoTracking()
                    .Where(hs => hs.Holidays.Any(h =>
                        h.Date >= startDateTime &&
                        h.Date <= endDateTime &&
                        h.States.Any(s => s.StateCode == state)))
                    .Include(hs => hs.Holidays)
                        .ThenInclude(h => h.States)
                    .ToListAsync(cancellationToken);

                // Convert to domain models
                var result = new List<HolidaySequence>();

                foreach (var sequenceEntity in sequences)
                {
                    // Filter holidays to only those in the specified state and date range
                    var holidaysInState = sequenceEntity.Holidays
                        .Where(h => h.States.Any(s => s.StateCode == state) &&
                               h.Date >= startDateTime && h.Date <= endDateTime)
                        .ToList();

                    // Create domain models for each holiday
                    var holidayModels = holidaysInState
                        .Select(h => new PublicHoliday(
                            LocalDate.FromDateTime(h.Date),
                            h.Name,
                            h.States.Select(s => s.StateCode).ToList()
                        ))
                        .ToList();

                    // Create the sequence if it has holidays
                    if (holidayModels.Any())
                    {
                        result.Add(new HolidaySequence(sequenceEntity.Name, holidayModels));
                    }
                }

                _logger.LogDebug("Found {SequenceCount} holiday sequences for state {State} in date range",
                    result.Count, state);

                return result as IReadOnlyCollection<HolidaySequence>;
            }) ?? new List<HolidaySequence>();  // Return empty list if cache returns null
        }
    }
}