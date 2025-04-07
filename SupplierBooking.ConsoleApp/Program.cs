using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Testing;
using NodaTime.Text;
using SupplierBooking.Domain;
using SupplierBooking.Domain.Interfaces;
using SupplierBooking.Infrastructure.Data;
using SupplierBooking.Infrastructure.Services;

namespace SupplierBooking.ConsoleApp
{
    /// <summary>
    /// Console application to demonstrate supplier availability calculation
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        public static async Task Main(string[] args)
        {
            // Set up console culture for proper date formatting
            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-AU");

            Console.WriteLine("Supplier Availability Calculator");
            Console.WriteLine("===============================");
            Console.WriteLine();

            // Configure services
            var services = ConfigureServices();

            // Run the application
            await RunApplicationAsync(services);
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Configure logging
            services.AddLogging(configure => configure
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information));

            // Configure database
            services.AddDbContext<SupplierBookingContext>(options =>
                options.UseInMemoryDatabase("SupplierBooking"));

            // Configure caching
            services.AddMemoryCache();
            // Configure logging - modified to use proper extension method
            services.AddLogging(configure =>
            {
                configure.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // Configure application services
            services.AddSingleton<IClockProvider, ClockProvider>();
            services.AddScoped<IPublicHolidayProvider, PublicHolidayProvider>();
            services.AddScoped<IBusinessDayCalculator, BusinessDayCalculator>();

            // Configure options
            services.Configure<AvailabilityCalculationOptions>(options =>
            {
                options.TimeZoneId = "Australia/Sydney";
                options.CutoffTime = new LocalTime(12, 0, 0);
                options.BusinessDaysBeforeHoliday = 2;
                options.HolidayLookAheadDays = 60;
            });

            services.AddScoped<IAvailabilityCalculator, AvailabilityCalculator>();

            return services.BuildServiceProvider();
        }

        private static async Task RunApplicationAsync(IServiceProvider serviceProvider)
        {
            // Initial database setup
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SupplierBookingContext>();
                await dbContext.Database.EnsureCreatedAsync();
            }

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Check next available date using current time");
                Console.WriteLine("2. Check next available date using specified time");
                Console.WriteLine("3. Run test cases");
                Console.WriteLine("4. Run direct test implementation");
                Console.WriteLine("5. Exit");
                Console.Write("Option: ");

                var option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await CheckWithCurrentTimeAsync(serviceProvider);
                        break;
                    case "2":
                        await CheckWithSpecifiedTimeAsync(serviceProvider);
                        break;
                    case "3":
                        await RunTestCasesAsync(serviceProvider);
                        break;
                    case "4":
                        await TestHelper.RunTestCasesStandaloneAsync();
                        break;
                    case "5":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        private static async Task CheckWithCurrentTimeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var calculator = scope.ServiceProvider.GetRequiredService<IAvailabilityCalculator>();
            var clockProvider = scope.ServiceProvider.GetRequiredService<IClockProvider>();

            var state = GetStateInput();
            var currentTime = clockProvider.GetCurrentDateTime();

            Console.WriteLine($"Current time: {currentTime:yyyy-MM-dd HH:mm:ss}");

            var result = await calculator.GetNextAvailableDateAsync(currentTime, state);

            DisplayResult(result, currentTime);
        }

        private static async Task CheckWithSpecifiedTimeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var calculator = scope.ServiceProvider.GetRequiredService<IAvailabilityCalculator>();

            var state = GetStateInput();
            var referenceTime = GetDateTimeInput();

            if (referenceTime == null)
            {
                Console.WriteLine("Invalid date/time format");
                return;
            }

            var result = await calculator.GetNextAvailableDateAsync(referenceTime.Value, state);

            DisplayResult(result, referenceTime.Value);
        }

        private static async Task RunTestCasesAsync(IServiceProvider serviceProvider)
        {
            Console.WriteLine("\nRunning test cases:");
            Console.WriteLine("====================");

            // Create test cases
            var testCases = new[]
            {
                new TestCase
                {
                    Description = "Before cutoff (Tue 15 April 2025, 9:00am)",
                    ReferenceTime = new LocalDateTime(2025, 4, 15, 9, 0, 0)
                        .InZoneStrictly(DateTimeZoneProviders.Tzdb["Australia/Sydney"]),
                    ExpectedNextAvailableDate = new LocalDate(2025, 4, 16) // Wed 16 April
                },
                new TestCase
                {
                    Description = "Just before cutoff (Wed 16 April 2025, 11:59am)",
                    ReferenceTime = new LocalDateTime(2025, 4, 16, 11, 59, 0)
                        .InZoneStrictly(DateTimeZoneProviders.Tzdb["Australia/Sydney"]),
                    ExpectedNextAvailableDate = new LocalDate(2025, 4, 22) // Tue 22 April
                },
                new TestCase
                {
                    Description = "At cutoff (Wed 16 April 2025, 12:00pm)",
                    ReferenceTime = new LocalDateTime(2025, 4, 16, 12, 0, 0)
                        .InZoneStrictly(DateTimeZoneProviders.Tzdb["Australia/Sydney"]),
                    ExpectedNextAvailableDate = new LocalDate(2025, 4, 23) // Wed 23 April
                },
                new TestCase
                {
                    Description = "After cutoff (Thu 17 April 2025, 9:00am)",
                    ReferenceTime = new LocalDateTime(2025, 4, 17, 9, 0, 0)
                        .InZoneStrictly(DateTimeZoneProviders.Tzdb["Australia/Sydney"]),
                    ExpectedNextAvailableDate = new LocalDate(2025, 4, 23) // Wed 23 April
                }
            };

            const string testState = "NSW";

            foreach (var testCase in testCases)
            {
                try
                {
                    Console.WriteLine($"\nTest case: {testCase.Description}");
                    Console.WriteLine($"Reference time: {testCase.ReferenceTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"Expected next available date: {testCase.ExpectedNextAvailableDate:yyyy-MM-dd}");

                    // Create a new service provider for each test case with a fixed clock
                    var testServiceProvider = CreateTestServiceProvider(testCase.ReferenceTime);

                    // Make sure database is created for each test
                    using (var scope = testServiceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<SupplierBookingContext>();
                        await dbContext.Database.EnsureCreatedAsync();
                    }

                    // Get the calculator
                    using var testScope = testServiceProvider.CreateScope();
                    var calculator = testScope.ServiceProvider.GetRequiredService<IAvailabilityCalculator>();

                    // Run the test
                    var result = await calculator.GetNextAvailableDateAsync(
                        testCase.ReferenceTime, testState, CancellationToken.None);

                    // Display the result
                    Console.WriteLine($"Actual next available date: {result.NextAvailableDate:yyyy-MM-dd}");

                    if (result.WasAfterCutoff)
                    {
                        Console.WriteLine($"Was after cutoff: Yes");
                        Console.WriteLine($"Cutoff time: {result.CutoffDateTime:yyyy-MM-dd HH:mm:ss}");

                        Console.WriteLine("Affected holidays:");
                        foreach (var holiday in result.AffectedHolidays)
                        {
                            Console.WriteLine($"- {holiday.Date:yyyy-MM-dd}: {holiday.Name}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Was after cutoff: No");
                    }

                    if (result.NextAvailableDate == testCase.ExpectedNextAvailableDate)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✓ PASS");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ FAIL - Expected {testCase.ExpectedNextAvailableDate:yyyy-MM-dd} but got {result.NextAvailableDate:yyyy-MM-dd}");
                    }

                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error running test case: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    Console.ResetColor();
                }
            }
        }

        private static IServiceProvider CreateTestServiceProvider(ZonedDateTime referenceTime)
        {
            // Create a new service collection
            var services = new ServiceCollection();

            // Configure logging with minimal output
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Warning); // Only show warnings and errors
            });

            // Configure database with a unique name
            var dbName = $"TestDB_{Guid.NewGuid()}";
            services.AddDbContext<SupplierBookingContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // Configure caching
            services.AddMemoryCache();

            // Configure fake clock with the reference time
            var fakeClock = new FakeClock(referenceTime.ToInstant());
            services.AddSingleton<IClock>(fakeClock);
            services.AddSingleton<IClockProvider>(provider =>
                new ClockProvider(provider.GetRequiredService<IClock>()));

            // Configure application services
            services.AddScoped<IPublicHolidayProvider, PublicHolidayProvider>();
            services.AddScoped<IBusinessDayCalculator, BusinessDayCalculator>();

            // Configure options
            services.Configure<AvailabilityCalculationOptions>(options =>
            {
                options.TimeZoneId = "Australia/Sydney";
                options.CutoffTime = new LocalTime(12, 0, 0);
                options.BusinessDaysBeforeHoliday = 2;
                options.HolidayLookAheadDays = 60;
            });

            // Use our fixed implementation for tests
            services.AddScoped<IAvailabilityCalculator, AvailabilityCalculator>();

            // Build and return the service provider
            return services.BuildServiceProvider();
        }


        private static string GetStateInput()
        {
            string[] validStates = { "NSW", "VIC", "QLD", "WA", "SA", "TAS", "ACT", "NT" };

            while (true)
            {
                Console.Write("\nEnter state code (NSW, VIC, QLD, WA, SA, TAS, ACT, NT): ");
                var state = Console.ReadLine()?.Trim().ToUpper();

                if (validStates.Contains(state))
                {
                    return state;
                }

                Console.WriteLine("Invalid state code. Please try again.");
            }
        }

        private static ZonedDateTime? GetDateTimeInput()
        {
            Console.Write("\nEnter reference date and time (yyyy-MM-dd HH:mm): ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            try
            {
                // Use Parse instead of TryParse
                var pattern = LocalDateTimePattern.CreateWithInvariantCulture("yyyy-MM-dd HH:mm");
                var parseResult = pattern.Parse(input);

                if (parseResult.Success)
                {
                    return parseResult.Value.InZoneStrictly(DateTimeZoneProviders.Tzdb["Australia/Sydney"]);
                }
            }
            catch (Exception)
            {
                // Parse failed
            }

            return null;
        }

        private static void DisplayResult(SupplierAvailabilityResult result, ZonedDateTime referenceTime)
        {
            Console.WriteLine();
            Console.WriteLine("Results:");
            Console.WriteLine("=========");
            Console.WriteLine($"Reference time: {referenceTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Next available date: {result.NextAvailableDate:yyyy-MM-dd} ({result.NextAvailableDate.DayOfWeek})");

            if (result.CutoffDateTime.HasValue)
            {
                Console.WriteLine($"Cutoff date/time: {result.CutoffDateTime.Value:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Past cutoff: {(result.WasAfterCutoff ? "Yes" : "No")}");
            }

            Console.WriteLine();
            Console.WriteLine("Affected holidays:");

            if (result.AffectedHolidays.Any())
            {
                foreach (var holiday in result.AffectedHolidays.OrderBy(h => h.Date))
                {
                    Console.WriteLine($"- {holiday.Date:yyyy-MM-dd} ({holiday.Date.DayOfWeek}): {holiday.Name}");
                }
            }
            else
            {
                Console.WriteLine("None");
            }
        }
    }

    /// <summary>
    /// Represents a test case for availability calculation
    /// </summary>
    public class TestCase
    {
        /// <summary>
        /// Gets or sets the description of the test case
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the reference date and time
        /// </summary>
        public ZonedDateTime ReferenceTime { get; set; }

        /// <summary>
        /// Gets or sets the expected next available date
        /// </summary>
        public LocalDate ExpectedNextAvailableDate { get; set; }
    }
}