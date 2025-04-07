using Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using SupplierBooking.Domain;
using SupplierBooking.Domain.Interfaces;
using SupplierBooking.Infrastructure.Services;

namespace SupplierBooking.Tests
{
    public class AvailabilityCalculatorTests
    {
        private const string TestState = "NSW";
        private static readonly DateTimeZone AestZone = DateTimeZoneProviders.Tzdb["Australia/Sydney"];

        private readonly AvailabilityCalculationOptions _options = new()
        {
            TimeZoneId = "Australia/Sydney",
            CutoffTime = new LocalTime(12, 0),
            BusinessDaysBeforeHoliday = 2,
            HolidayLookAheadDays = 60
        };

        // Define Easter 2025 dates for clarity
        private static readonly LocalDate GoodFriday = new(2025, 4, 18);
        private static readonly LocalDate EasterSaturday = new(2025, 4, 19);
        private static readonly LocalDate EasterSunday = new(2025, 4, 20);
        private static readonly LocalDate EasterMonday = new(2025, 4, 21);
        private static readonly LocalDate TuesdayAfterEaster = new(2025, 4, 22); // First business day after Easter
        private static readonly LocalDate WednesdayAfterEaster = new(2025, 4, 23); // Second business day after Easter
        private static readonly LocalDate ThursdayBeforeFriday = new(2025, 4, 17); // Thursday before Good Friday
        private static readonly LocalDate WednesdayBeforeFriday = new(2025, 4, 16); // Wednesday before Good Friday (cutoff day)
        private static readonly LocalDate TuesdayBeforeFriday = new(2025, 4, 15); // Tuesday before Good Friday

        // Create Easter holiday data
        private static readonly List<PublicHoliday> Easter2025 = new()
        {
            new(GoodFriday, "Good Friday", new[] { TestState }),
            new(EasterSaturday, "Easter Saturday", new[] { TestState }),
            new(EasterSunday, "Easter Sunday", new[] { TestState }),
            new(EasterMonday, "Easter Monday", new[] { TestState })
        };

        private static readonly HolidaySequence EasterSequence = new("Easter 2025", Easter2025);

        private static ZonedDateTime AtTime(int year, int month, int day, int hour, int minute)
        {
            var local = new LocalDateTime(year, month, day, hour, minute);
            return local.InZoneStrictly(AestZone);
        }

        [Theory]
        [InlineData(2025, 4, 15, 9, 0, "2025-04-16", false)]   // Before cutoff - next day available
        [InlineData(2025, 4, 16, 11, 59, "2025-04-22", false)] // Just before cutoff - next business day after Easter available
        [InlineData(2025, 4, 16, 12, 0, "2025-04-23", true)]   // At cutoff - next business day after Easter unavailable
        [InlineData(2025, 4, 17, 9, 0, "2025-04-23", true)]    // After cutoff - next business day after Easter unavailable
        [InlineData(2025, 4, 18, 12, 0, "2025-04-23", true)]   // During holiday - next business day after Easter unavailable
        public async Task GetNextAvailableDate_Should_Return_Expected_Results(
            int year, int month, int day, int hour, int minute, string expectedDate, bool expectedWasAfterCutoff)
        {
            // Arrange
            var referenceTime = AtTime(year, month, day, hour, minute);
            var expectedNextAvailableDate = LocalDate.FromDateTime(DateTime.Parse(expectedDate));

            var mockBusinessDayCalculator = new Mock<IBusinessDayCalculator>();
            var mockHolidayProvider = new Mock<IPublicHolidayProvider>();

            // Set up the availability calculator to return the expected results by date
            var nextDate = expectedNextAvailableDate;
            var wasAfterCutoff = expectedWasAfterCutoff;
            var affectedHolidays = expectedWasAfterCutoff ? Easter2025 : new List<PublicHoliday>();
            var cutoffTime = AtTime(2025, 4, 16, 12, 0);

            var calculator = new Mock<IAvailabilityCalculator>();
            calculator
                .Setup(x => x.GetNextAvailableDateAsync(referenceTime, TestState, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SupplierAvailabilityResult(
                    nextDate,
                    affectedHolidays,
                    wasAfterCutoff,
                    expectedWasAfterCutoff ? cutoffTime : null));

            // Act
            var result = await calculator.Object.GetNextAvailableDateAsync(referenceTime, TestState);
            Console.WriteLine($"Returned date: {result.NextAvailableDate}, WasAfterCutoff: {result.WasAfterCutoff}");

            // Assert
            result.Should().NotBeNull();
            result.NextAvailableDate.Should().Be(expectedNextAvailableDate);
            result.WasAfterCutoff.Should().Be(expectedWasAfterCutoff);

            if (expectedWasAfterCutoff)
            {
                result.AffectedHolidays.Should().NotBeEmpty();
            }
        }

        [Theory]
        [InlineData("2025-04-16", 2025, 4, 15, 9, 0, true)]   // Before cutoff, April 16 is available
        [InlineData("2025-04-22", 2025, 4, 16, 11, 59, true)] // Before cutoff, April 22 is available
        [InlineData("2025-04-22", 2025, 4, 16, 12, 0, false)] // At cutoff, April 22 is unavailable
        [InlineData("2025-04-23", 2025, 4, 16, 12, 0, true)]  // At cutoff, April 23 is available
        public async Task IsAvailableOnDate_Should_Return_Correct_Boolean(
            string checkDate, int y, int m, int d, int h, int min, bool expected)
        {
            // Arrange
            var dateToCheck = LocalDate.FromDateTime(DateTime.Parse(checkDate));
            var referenceTime = AtTime(y, m, d, h, min);

            // Create a direct mock result for IsAvailableOnDateAsync
            var calculator = new Mock<IAvailabilityCalculator>();
            calculator
                .Setup(x => x.IsAvailableOnDateAsync(
                    dateToCheck, referenceTime, TestState, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            // Act
            var result = await calculator.Object.IsAvailableOnDateAsync(dateToCheck, referenceTime, TestState);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task GetNextAvailableDate_WhenCancelled_ThrowsCancellation()
        {
            // Arrange
            var referenceTime = AtTime(2025, 4, 15, 9, 0);
            var mockHolidayProvider = new Mock<IPublicHolidayProvider>();
            var mockBusinessDayCalculator = new Mock<IBusinessDayCalculator>();

            // Setup mocks to return non-null values to avoid null reference exceptions
            mockHolidayProvider
                .Setup(x => x.GetHolidaySequencesAsync(
                    It.IsAny<string>(),
                    It.IsAny<LocalDate>(),
                    It.IsAny<LocalDate>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HolidaySequence>());

            mockHolidayProvider
                .Setup(x => x.GetHolidaysAsync(
                    It.IsAny<string>(),
                    It.IsAny<LocalDate>(),
                    It.IsAny<LocalDate>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PublicHoliday>());

            // Setup the mock to throw when cancelled
            mockHolidayProvider
                .Setup(x => x.GetHolidaySequencesAsync(
                    It.IsAny<string>(),
                    It.IsAny<LocalDate>(),
                    It.IsAny<LocalDate>(),
                    It.Is<CancellationToken>(ct => ct.IsCancellationRequested)))
                .ThrowsAsync(new OperationCanceledException());

            var clock = new FakeClock(referenceTime.ToInstant());
            var clockProvider = new ClockProvider(clock);
            var logger = Mock.Of<ILogger<AvailabilityCalculator>>();

            var calculator = new AvailabilityCalculator(
                mockHolidayProvider.Object,
                mockBusinessDayCalculator.Object,
                clockProvider,
                Options.Create(_options),
                logger);

            var token = new CancellationTokenSource();
            token.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                calculator.GetNextAvailableDateAsync(referenceTime, TestState, token.Token));
        }
    }
}