using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SNYC_Transport.Models;

public class Vehicles
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Name { get; set; }

    [Column("plate_number")]
    public string? PlateNumber { get; set; }

    public int Capacity { get; set; }

    public bool Active { get; set; } = true;
}