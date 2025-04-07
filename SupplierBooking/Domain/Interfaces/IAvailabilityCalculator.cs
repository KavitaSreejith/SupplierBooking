using NodaTime;

namespace SupplierBooking.Domain.Interfaces
{
    /// <summary>
    /// Interface for calculating supplier availability
    /// </summary>
    public interface IAvailabilityCalculator
    {
        /// <summary>
        /// Calculates the next available date for a supplier in a specific state
        /// </summary>
        /// <param name="referenceDateTime">The reference date and time (now)</param>
        /// <param name="state">The state to check holidays for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A result containing the next available date</returns>
        Task<SupplierAvailabilityResult> GetNextAvailableDateAsync(
            ZonedDateTime referenceDateTime,
            string state,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a supplier is available on a specific date
        /// </summary>
        /// <param name="checkDate">The date to check</param>
        /// <param name="referenceDateTime">The reference date and time (now)</param>
        /// <param name="state">The state to check holidays for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the supplier is available on the specified date, false otherwise</returns>
        Task<bool> IsAvailableOnDateAsync(
            LocalDate checkDate,
            ZonedDateTime referenceDateTime,
            string state,
            CancellationToken cancellationToken = default);
    }
}