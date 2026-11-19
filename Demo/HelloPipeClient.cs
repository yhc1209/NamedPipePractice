using System.Threading;
using System.Threading.Tasks;
using NamedPipePractice.Pipe;

namespace NamedPipePractice.Demo;

public class HelloPipeClient : PipeClient
{
    public HelloPipeClient(string serverName = ".") : base(HelloPipeServer.ServiceName, serverName)
    {
    }

    public async Task<ReplyPackage> SendHelloAsync(HelloAction action, HelloInfo info, CancellationToken token = default)
    {
        return await SendAsync(new QueryPackage
        {
            Action = (int)action,
            Data = info
        }, token).ConfigureAwait(false);
    }
}
