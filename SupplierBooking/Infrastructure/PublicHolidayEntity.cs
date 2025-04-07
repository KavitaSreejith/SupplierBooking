using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace SupplierBooking.Infrastructure.Data
{
    /// <summary>
    /// Entity representing a public holiday
    /// </summary>
    [Table("PublicHolidays")]
    public class PublicHolidayEntity
    {
        /// <summary>
        /// Gets or sets the primary key
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the date of the holiday
        /// </summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the name of the holiday
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sequence ID for grouped holidays
        /// </summary>
        public int? HolidaySequenceId { get; set; }

        /// <summary>
        /// Navigation property for holiday sequence
        /// </summary>
        [ForeignKey(nameof(HolidaySequenceId))]
        public HolidaySequenceEntity HolidaySequence { get; set; }

        /// <summary>
        /// Navigation property for holiday state mappings
        /// </summary>
        public ICollection<HolidayStateEntity> States { get; set; } = new HashSet<HolidayStateEntity>();
    }
}