using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SNYC_Transport.Models;

namespace SNYC_Transport.Models;

public class TripPassengers
{
    [Key]
    public Guid Id { get; set; }

    [Column("trip_id")]
    public Guid TripId { get; set; }

    [Column("request_id")]
    public Guid RequestId { get; set; }

    [Column("pickup_order")]
    public int? PickupOrder { get; set; }

    // constraints
    [ForeignKey(nameof(TripId))]
    public Trips? Trip { get; set; }

    [ForeignKey(nameof(RequestId))]
    public TransportRequests? Request { get; set; }
}