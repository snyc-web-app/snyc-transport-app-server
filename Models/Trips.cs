using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SNYC_Transport.Models;

public class Trips
{
    [Key]
    public Guid Id { get; set; }

    [Column("vehicle_id")]
    public Guid? VehicleId { get; set; }
    public Vehicles? Vehicle { get; set; }

    [Column("driver_id")]
    public Guid? DriverId { get; set; }

    [Column("trip_date")]
    public DateTime TripDate { get; set; }

    [Column("trip_status")]
    public string TripStatus { get; set; } = "planned";

    [Column("start_location")]
    public string StartLocation { get; set; } = "Victoria";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}