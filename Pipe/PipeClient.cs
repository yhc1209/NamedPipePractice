using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipePractice.Pipe;

public class PipeClient
{
    private string PipeName { get; init; }
    private string ServerName { get; init; }

    public int DefaultTimeoutMilliseconds { get; set; } = 5000;

    public PipeClient(string pipeName, string serverName = ".")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);

        PipeName = pipeName;
        ServerName = serverName;
    }

    public Task<ReplyPackage> SendAsync(QueryPackage query, CancellationToken token = default)
    {
        return SendAsync(query, DefaultTimeoutMilliseconds, token);
    }

    public async Task<ReplyPackage> SendAsync(QueryPackage query, int timeoutMilliseconds, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        using var pipe = CreatePipeClient();
        using var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
        var linkedToken = linkedCts.Token;

        try
        {
            await pipe.ConnectAsync(linkedToken).ConfigureAwait(false);

            using (var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true))
            {
                string request = JsonSerializer.Serialize(query);
                await writer.WriteLineAsync(request.AsMemory(), linkedToken).ConfigureAwait(false);
                await writer.FlushAsync(linkedToken).ConfigureAwait(false);
            }

            ReplyPackage reply;
            using (var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true))
            {
                string response = await reader.ReadLineAsync(linkedToken).ConfigureAwait(false) ??
                    throw new PipeClientException(PipeClientExcpCode.NoReplyData);

                reply = JsonSerializer.Deserialize<ReplyPackage>(response) ??
                    throw new PipeClientException(PipeClientExcpCode.NoReplyPackage);
            }

            return reply;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !token.IsCancellationRequested)
        {
            throw new TimeoutException($"The operation timed out after {timeoutMilliseconds} ms while communicating with pipe '{PipeName}'.");
        }
    }

    public ReplyPackage Send(QueryPackage query, int timeoutMilliseconds = 5000)
    {
        return SendAsync(query, timeoutMilliseconds).GetAwaiter().GetResult();
    }

    private NamedPipeClientStream CreatePipeClient()
    {
        return new NamedPipeClientStream(
            ServerName,
            PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough
        );
    }
}
