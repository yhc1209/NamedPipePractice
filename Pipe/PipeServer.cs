using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NamedPipePractice.Pipe;

public class PipeServer
{
    public const int MAX_NUMBER_OF_CONNECTION = 5;

    private string PipeName { get; init; }
    private readonly PackageHandler packageHandler;
    private Task? task = null;
    private readonly List<Task> connections;

    public PipeServer(string name, PackageHandler handler)
    {
        PipeName = name;
        packageHandler = handler;
        connections = new List<Task>();
    }

    public void StartListen(CancellationToken token)
    {
        if (task != null && !task.IsCompleted)
            throw new InvalidOperationException("The pipe server is listening.");

        task = StartListenAsync(token);
    }

    private async Task StartListenAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            connections.RemoveAll(conn => conn.IsCompleted);

            if (connections.Count < MAX_NUMBER_OF_CONNECTION)
                connections.Add(TryHandleConnectionAsync(token));
            else
                await Task.WhenAny(connections).ConfigureAwait(false);
        }
    }

    private async Task TryHandleConnectionAsync(CancellationToken token)
    {
        try { await HandleConnectionAsync(token).ConfigureAwait(false); }
        catch (PipeServerException excp)
        {
            Console.Error.WriteLine(
                "Failed to handle query! ({0})",
                excp.Code
            );
        }
        catch (Exception excp)
        {
            Console.Error.WriteLine(
                "Exception thrown while handling query! ({0} - {1})",
                excp.GetType(), excp.Message
            );
            Console.Error.WriteLine(excp.StackTrace);
        }
    }

    private async Task HandleConnectionAsync(CancellationToken token)
    {
        using (NamedPipeServerStream pipe = CreatePipeServer())
        {
            await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);

            QueryPackage query;
            using (var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true))
            {
                string request = await reader.ReadLineAsync(token).ConfigureAwait(false) ??
                    throw new PipeServerException(PipeServerExcpCode.NoQueryData);

                query = JsonSerializer.Deserialize<QueryPackage>(request) ??
                    throw new PipeServerException(PipeServerExcpCode.NoQueryPackage);
            }

            ReplyPackage reply = packageHandler.Invoke(query);
            using (var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true))
            {
                string response = JsonSerializer.Serialize(reply);
                await writer.WriteLineAsync(response.AsMemory(), token).ConfigureAwait(false);
            }
        }
    }

    private NamedPipeServerStream CreatePipeServer()
    {
        return new NamedPipeServerStream(
            PipeName,
            PipeDirection.InOut,
            MAX_NUMBER_OF_CONNECTION,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough
        );
    }
}