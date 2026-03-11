using SNYC_Transport.Models;

namespace SNYC_Transport.Services;

public interface ITransportRequestService
{
    Task<IReadOnlyList<TransportRequests>> GetAllAsync();
    Task<TransportRequests> CreateAsync(TransportRequestInput input);
    Task<bool> UpdateStatusAsync(Guid id, string status);
    Task<bool> DeleteAsync(Guid id);
}
