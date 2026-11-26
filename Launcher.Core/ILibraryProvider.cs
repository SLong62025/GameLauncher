using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher.Core;
//Use for Launcher.Providers
public interface ILibraryProvider {
    GameStore Store { get; }

    Task<IReadOnlyList<Game>> ScanInstalledAsync(CancellationToken ct = default);

    void Launch(Game game);
}