using Microsoft.EntityFrameworkCore;
using SupplierBooking.Infrastructure.Data;

public class SupplierBookingContext : DbContext
{
    /// <summary>
    /// Gets or sets the public holidays DbSet
    /// </summary>
    public DbSet<PublicHolidayEntity> PublicHolidays { get; set; }

    /// <summary>
    /// Gets or sets the holiday sequences DbSet
    /// </summary>
    public DbSet<HolidaySequenceEntity> HolidaySequences { get; set; }

    /// <summary>
    /// Gets or sets the holiday states DbSet
    /// </summary>
    public DbSet<HolidayStateEntity> HolidayStates { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SupplierBookingContext"/> class
    /// </summary>
    public SupplierBookingContext(DbContextOptions<SupplierBookingContext> options)
        : base(options)
    {
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure indexes
        modelBuilder.Entity<PublicHolidayEntity>()
            .HasIndex(p => p.Date);

        modelBuilder.Entity<HolidayStateEntity>()
            .HasIndex(hs => hs.StateCode);

        // Configure relationships
        modelBuilder.Entity<PublicHolidayEntity>()
            .HasMany(p => p.States)
            .WithOne(s => s.Holiday)
            .HasForeignKey(s => s.HolidayId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HolidaySequenceEntity>()
            .HasMany(hs => hs.Holidays)
            .WithOne(p => p.HolidaySequence)
            .HasForeignKey(p => p.HolidaySequenceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure known holiday data
        SeedHolidayData(modelBuilder);
    }

    private void SeedHolidayData(ModelBuilder modelBuilder)
    {
        // Create Easter 2025 sequence
        var easterSequence = new HolidaySequenceEntity
        {
            Id = 1,
            Name = "Easter 2025"
        };

        modelBuilder.Entity<HolidaySequenceEntity>().HasData(easterSequence);

        // Create Easter 2025 holidays
        var easterHolidays = new[]
        {
            new PublicHolidayEntity
            {
                Id = 1,
                Name = "Good Friday",
                Date = new DateTime(2025, 4, 18),
                HolidaySequenceId = 1
            },
            new PublicHolidayEntity
            {
                Id = 2,
                Name = "Easter Saturday",
                Date = new DateTime(2025, 4, 19),
                HolidaySequenceId = 1
            },
            new PublicHolidayEntity
            {
                Id = 3,
                Name = "Easter Sunday",
                Date = new DateTime(2025, 4, 20),
                HolidaySequenceId = 1
            },
            new PublicHolidayEntity
            {
                Id = 4,
                Name = "Easter Monday",
                Date = new DateTime(2025, 4, 21),
                HolidaySequenceId = 1
            }
        };

        modelBuilder.Entity<PublicHolidayEntity>().HasData(easterHolidays);

        // Create holiday state mappings for all Australian states
        var states = new[] { "NSW", "VIC", "QLD", "WA", "SA", "TAS", "ACT", "NT" };

        // Create separate collection for holiday states to ensure unique IDs
        var holidayStates = new List<HolidayStateEntity>();
        int stateId = 1;

        // Add states for Easter holidays
        foreach (var holiday in easterHolidays)
        {
            foreach (var state in states)
            {
                holidayStates.Add(new HolidayStateEntity
                {
                    Id = stateId++,
                    HolidayId = holiday.Id,
                    StateCode = state
                });
            }
        }

        // Add ANZAC Day 2025
        var anzacDay = new PublicHolidayEntity
        {
            Id = 5,
            Name = "ANZAC Day",
            Date = new DateTime(2025, 4, 25)
        };

        modelBuilder.Entity<PublicHolidayEntity>().HasData(anzacDay);

        // Add states for ANZAC Day (using the continuing ID sequence)
        foreach (var state in states)
        {
            holidayStates.Add(new HolidayStateEntity
            {
                Id = stateId++,
                HolidayId = anzacDay.Id,
                StateCode = state
            });
        }

        // Add all state entities in one go
        modelBuilder.Entity<HolidayStateEntity>().HasData(holidayStates);
    }
}