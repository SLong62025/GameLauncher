using System.Threading;
using System.Threading.Tasks;
using Launcher.Core;

namespace Launcher.Providers.Art;

public interface IArtSource {

    string Name { get; }

    bool CanHandle(Game game);
    
    Task<byte[]?> FetchFile(Game game, CancellationToken ct);
}
