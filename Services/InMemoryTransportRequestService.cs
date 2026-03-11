using SNYC_Transport.Models;

namespace SNYC_Transport.Services;

public class InMemoryTransportRequestService : ITransportRequestService
{
    private readonly List<TransportRequests> requests = new();

    public Task<IReadOnlyList<TransportRequests>> GetAllAsync()
    {
        IReadOnlyList<TransportRequests> result = requests
            .OrderByDescending(r => r.UpdatedAtUtc)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<TransportRequests> CreateAsync(TransportRequestInput input)
    {
        var request = new TransportRequests
        {
            UserId = input.UserId ?? Guid.Empty,
            Destination = input.Destination.Trim(),
            passengerCount = input.PassengerCount ?? 1,
            status = "Pending",
            complianceStatus = "Pending",
            adminStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        requests.Add(request);
        return Task.FromResult(request);
    }

    public Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        var request = requests.FirstOrDefault(r => r.Id == id);
        if (request is null)
        {
            return Task.FromResult(false);
        }

        request.status = status;
        request.adminStatus = status;
        request.UpdatedAtUtc = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var request = requests.FirstOrDefault(r => r.Id == id);
        if (request is null)
        {
            return Task.FromResult(false);
        }

        requests.Remove(request);
        return Task.FromResult(true);
    }
}
