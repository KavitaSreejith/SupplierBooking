using SupplierBooking.Infrastructure.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Entity representing a holiday sequence (e.g., Easter)
/// </summary>
[Table("HolidaySequences")]
public class HolidaySequenceEntity
{
    /// <summary>
    /// Gets or sets the primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the holiday sequence
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// Navigation property for holidays in this sequence
    /// </summary>
    public ICollection<PublicHolidayEntity> Holidays { get; set; } = new HashSet<PublicHolidayEntity>();
}

/// <summary>
/// Entity representing a state where a holiday is observed
/// </summary>
[Table("HolidayStates")]
public class HolidayStateEntity
{
    /// <summary>
    /// Gets or sets the primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the holiday ID
    /// </summary>
    public int HolidayId { get; set; }

    /// <summary>
    /// Gets or sets the state code
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string StateCode { get; set; }

    /// <summary>
    /// Navigation property for the holiday
    /// </summary>
    [ForeignKey(nameof(HolidayId))]
    public PublicHolidayEntity Holiday { get; set; }
}