namespace SNYC_Transport.Models;

public class TransportRequests
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public string Destination { get; set; } = string.Empty;
    public int passengerCount { get; set; } = 1;
    public string status { get; set; } = "Pending";
    public string complianceStatus { get; set; } = "Pending";
    public string complianceReviewedBy { get; set; } = string.Empty;
    // Numeric value representing the timestamp of when compliance was reviewed
    public string complianceReviewedAt { get; set; } = string.Empty;

    public string adminStatus { get; set; } = "Pending";
    public string adminReviewedBy { get; set; } = string.Empty;
    // Numeric value representing the timestamp of when admin reviewed the request
    public string adminReviewedAt { get; set; } = string.Empty;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    // public string Name { get; set; } = string.Empty;
    // public int Age { get; set; }
    // public string Address { get; set; } = string.Empty;
    // public string PhoneNumber { get; set; } = string.Empty;
    // public string Status { get; set; } = "Pending";
}
