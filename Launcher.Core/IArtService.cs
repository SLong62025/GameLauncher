using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Core;

//Async function
public interface IArtService {
    Task<string?> GetCoverPath(Game game, CancellationToken ct = default);
}
