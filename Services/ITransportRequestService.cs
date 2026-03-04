using SNYC_Transport.Models;

namespace SNYC_Transport.Services;

public interface ITransportRequestService
{
    Task<IReadOnlyList<TransportRequest>> GetAllAsync();
    Task<TransportRequest> CreateAsync(TransportRequestInput input);
    Task<bool> UpdateStatusAsync(Guid id, string status);
    Task<bool> DeleteAsync(Guid id);
}
