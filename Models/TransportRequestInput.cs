using System.ComponentModel.DataAnnotations;

namespace SNYC_Transport.Models;

public class TransportRequestInput
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Destination { get; set; } = string.Empty;

    [Range(1, 20)]
    public int? PassengerCount { get; set; } = 1;

    public Guid? UserId { get; set; }
}
