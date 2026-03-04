using SNYC_Transport.Models;

namespace SNYC_Transport.Services;

public class InMemoryTransportRequestService : ITransportRequestService
{
    private readonly List<TransportRequest> requests = new();

    public Task<IReadOnlyList<TransportRequest>> GetAllAsync()
    {
        IReadOnlyList<TransportRequest> result = requests
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<TransportRequest> CreateAsync(TransportRequestInput input)
    {
        var request = new TransportRequest
        {
            Name = input.Name.Trim(),
            Age = input.Age ?? 0,
            Address = input.Address.Trim(),
            PhoneNumber = input.PhoneNumber.Trim()
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

        request.Status = status;
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
