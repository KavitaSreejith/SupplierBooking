using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupplierBooking.Domain.Interfaces;
using SupplierBooking.Infrastructure.Services;
using System;

namespace SupplierBooking
{
    /// <summary>
    /// Extension methods for configuring dependency injection
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers all services needed for supplier availability calculation
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddSupplierBookingServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register configuration
            services.Configure<AvailabilityCalculationOptions>(options =>
                configuration.GetSection("AvailabilityCalculation").Bind(options));

            // Register database with optimized configuration
            services.AddDbContext<SupplierBookingContext>(options =>
            {
                // This would typically use a real database, but for this example we'll use in-memory
                options.UseInMemoryDatabase("SupplierBooking");

                // Performance optimizations for in-memory database
                options.EnableSensitiveDataLogging(false);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                // For production, you would use something like this:
                //var connectionString = configuration.GetConnectionString("DefaultConnection");
                //options.UseSqlServer(connectionString, sqlOptions =>
                //{
                //    sqlOptions.EnableRetryOnFailure(3);
                //    sqlOptions.CommandTimeout(30);
                //    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                //});
            });

            // Register enhanced caching
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 2048; // Limit cache size to prevent memory issues
                options.CompactionPercentage = 0.25; // Compact by 25% when limit reached
            });

            // Register domain services with appropriate lifecycles
            services.AddScoped<IPublicHolidayProvider, PublicHolidayProvider>();
            services.AddScoped<IBusinessDayCalculator, BusinessDayCalculator>();
            services.AddSingleton<IClockProvider, ClockProvider>(); // Singleton for stateless provider
            services.AddScoped<IAvailabilityCalculator, AvailabilityCalculator>();

            return services;
        }
    }
}