using System.Threading;
using System.Threading.Tasks;

namespace NEventSocket.Examples.NetCore
{
    public interface ICommandLineTask<in TParameters>
    {
        Task Run(TParameters parameters, CancellationToken token);
    }

    public interface ICommandLineTask
    {
        Task Run(CancellationToken cancellationToken);
    }
}
