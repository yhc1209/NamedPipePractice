using System.Threading;
using System.Threading.Tasks;
using NamedPipePractice.Pipe;

namespace NamedPipePractice.Demo;

public class HelloPipeClient : PipeClient
{
    public string? AuthToken { get; set; }

    public HelloPipeClient(string serverName = ".") : base(HelloPipeServer.ServiceName, serverName)
    {
    }

    public async Task<ReplyPackage> SendHelloAsync(HelloAction action, HelloInfo info, string? authToken = null, CancellationToken token = default)
    {
        return await SendAsync(new QueryPackage
        {
            Action = (int)action,
            Token = authToken ?? AuthToken,
            Data = info
        }, token).ConfigureAwait(false);
    }

    public Task<ReplyPackage> SendHelloAsync(HelloAction action, HelloInfo info, CancellationToken token)
    {
        return SendHelloAsync(action, info, null, token);
    }
}
