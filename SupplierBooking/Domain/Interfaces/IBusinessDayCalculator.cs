using NodaTime;

namespace SupplierBooking.Domain.Interfaces
{
    /// <summary>
    /// Interface for business day calculations
    /// </summary>
    public interface IBusinessDayCalculator
    {
        /// <summary>
        /// Gets the previous business day before the specified date
        /// </summary>
        /// <param name="date">The reference date</param>
        /// <param name="state">The state to check holidays for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The previous business day</returns>
        Task<LocalDate> GetPreviousBusinessDayAsync(
            LocalDate date,
            string state,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the next business day after the specified date
        /// </summary>
        /// <param name="date">The reference date</param>
        /// <param name="state">The state to check holidays for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The next business day</returns>
        Task<LocalDate> GetNextBusinessDayAsync(
            LocalDate date,
            string state,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the specified date is a business day
        /// </summary>
        /// <param name="date">The date to check</param>
        /// <param name="state">The state to check holidays for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the date is a business day, false otherwise</returns>
        Task<bool> IsBusinessDayAsync(
            LocalDate date,
            string state,
            CancellationToken cancellationToken = default);
    }
}