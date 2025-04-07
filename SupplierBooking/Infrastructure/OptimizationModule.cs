using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SupplierBooking.Domain.Interfaces;
using SupplierBooking.Infrastructure.Services;
using System;

namespace SupplierBooking.Infrastructure
{
	/// <summary>
	/// Module to register optimized services with dependency injection
	/// </summary>
	public static class OptimizationModule
	{
		/// <summary>
		/// Registers optimized services for the application
		/// </summary>
		/// <param name="services">The service collection</param>
		/// <returns>The service collection for chaining</returns>
		public static IServiceCollection AddOptimizedServices(this IServiceCollection services)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			// Replace standard implementations with optimized ones
			services.AddScoped<IBusinessDayCalculator, BusinessDayCalculator>();
			services.AddScoped<IAvailabilityCalculator, AvailabilityCalculator>();

			// Configure enhanced memory cache with size limits
			services.AddMemoryCache(options =>
			{
				options.SizeLimit = 2048; // 2048 entries (adjust based on usage)
				options.CompactionPercentage = 0.25; // Remove 25% when limit is reached
			});

			// Configure efficient logging
			services.AddLogging(config =>
			{
				config.AddFilter("SupplierBooking.Infrastructure", LogLevel.Information);
				config.AddFilter("SupplierBooking.Domain", LogLevel.Warning);
				config.AddFilter("Microsoft", LogLevel.Warning);
			});

			return services;
		}

		/// <summary>
		/// Updates Program.cs to use optimized testing
		/// </summary>
		public static IServiceCollection UseOptimizedTesting(this IServiceCollection services)
		{
			// Register the optimized calculator for tests
			services.AddScoped<IAvailabilityCalculator, AvailabilityCalculator>();
			services.AddScoped<IBusinessDayCalculator, BusinessDayCalculator>();

			return services;
		}
	}
}