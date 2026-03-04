using System.ComponentModel.DataAnnotations;

namespace SNYC_Transport.Models;

public class TransportRequestInput
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(10, 99)]
    public int? Age { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;
}
