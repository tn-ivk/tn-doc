using System.Threading;
using System.Threading.Tasks;

namespace TN_Doc.Services;

public interface IStatusProvider
{
    Task<StatusResponse> GetStatusAsync(CancellationToken ct = default);
}


