using System;
using System.Threading;
using System.Threading.Tasks;
using NamedPipePractice.Pipe;

namespace NamedPipePractice.Demo;

public class HelloPipeClient : PipeClient
{
    public string AuthToken
    {
        get
        {
            if (helloToken is null || !helloToken.TryCheckException())
            {
                helloToken = new HelloToken(
                    nameof(HelloPipeClient),
                    nameof(HelloPipeClient),
                    Environment.ProcessId,
                    30 * 60 // 30 minutes
                );
            }

            return helloToken.GetTokenString(hashKey);
        }
    }

    private HelloToken? helloToken = null;
    private readonly byte[] hashKey = new byte[HelloToken.HASH_KEY_LENGTH];

    public HelloPipeClient(string serverName = ".") : base(HelloPipeServer.ServiceName, serverName)
    {
        var query = new QueryPackage
        {
            Action = (int)HelloAction.Register,
            Token = null,
            Data = new RegisterQueryInfo()
        };
        ReplyPackage reply = SendAsync(query).Result;
        string key = reply.GetData<RegisterReplyInfo>()?.Key ??
            throw new Exception("Failed to initalize.");
        hashKey = Convert.FromBase64String(key);
    }

    public async Task<ReplyPackage> SendHelloAsync(HelloAction action, HelloInfo info, string? authToken = null, CancellationToken token = default)
    {
        return await SendAsync(new QueryPackage
        {
            Action = (int)action,
            Token = authToken,
            Data = info
        }, token).ConfigureAwait(false);
    }

    public Task<ReplyPackage> SendHelloAsync(HelloAction action, HelloInfo info, CancellationToken token)
    {
        return SendHelloAsync(action, info, null, token);
    }
}
