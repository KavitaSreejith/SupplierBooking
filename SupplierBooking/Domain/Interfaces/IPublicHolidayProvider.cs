using Domain;
using NodaTime;

namespace SupplierBooking.Domain.Interfaces
{
    /// <summary>
    /// Interface for retrieving public holidays
    /// </summary>
    public interface IPublicHolidayProvider
    {
        /// <summary>
        /// Gets public holidays for a specific state within a date range
        /// </summary>
        /// <param name="state">The state to get holidays for</param>
        /// <param name="startDate">The start date of the range</param>
        /// <param name="endDate">The end date of the range</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of public holidays</returns>
        Task<IReadOnlyCollection<PublicHoliday>> GetHolidaysAsync(
            string state,
            LocalDate startDate,
            LocalDate endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets holiday sequences for a specific state within a date range
        /// </summary>
        /// <param name="state">The state to get holiday sequences for</param>
        /// <param name="startDate">The start date of the range</param>
        /// <param name="endDate">The end date of the range</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of holiday sequences</returns>
        Task<IReadOnlyCollection<HolidaySequence>> GetHolidaySequencesAsync(
            string state,
            LocalDate startDate,
            LocalDate endDate,
            CancellationToken cancellationToken = default);
    }
}