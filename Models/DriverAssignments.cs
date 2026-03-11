using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SNYC_Transport.Models;

public class DriverAssignments
{
    [Key]
    public Guid Id { get; set; }

    [Column("request_id")]
    public Guid RequestId { get; set; }

    [Column("driver_id")]
    public Guid DriverId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "assigned";

    [ForeignKey("RequestId")]
    public TransportRequests? Request { get; set; }
}