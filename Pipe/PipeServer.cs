using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Principal;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace NamedPipePractice.Pipe;

public partial class PipeServer
{
    public const int MAX_NUMBER_OF_CONNECTION = 5;

    private string PipeName { get; init; }
    private PackageHandler? packageHandler = null;
    private Task? task = null;
    private readonly List<Task> connections;

    public PipeServer(string name, PackageHandler? handler = null)
    {
        PipeName = name;
        packageHandler = handler;
        connections = new List<Task>();
    }

    public void SetHandler(PackageHandler handler)
    {
        packageHandler = handler;
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
        catch (OperationCanceledException) { }
        catch (UnauthorizedAccessException excp)
        {
            Console.Error.WriteLine(
                "Failed to handle the pipe! ({0} - {1})",
                excp.GetType(), excp.Message
            );
            Console.Error.WriteLine(excp.StackTrace);
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
            int clientPid = GetPipeClientPid(pipe.SafePipeHandle);

            QueryPackage query;
            using (var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true))
            {
                string request = await reader.ReadLineAsync(token).ConfigureAwait(false) ??
                    throw new PipeServerException(PipeServerExcpCode.NoQueryData);

                query = JsonSerializer.Deserialize<QueryPackage>(request) ??
                    throw new PipeServerException(PipeServerExcpCode.NoQueryPackage);
            }

            ReplyPackage reply = packageHandler?.Invoke(clientPid, query) ??
                throw new PipeServerException(PipeServerExcpCode.NoQueryHandler);
            using (var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true))
            {
                string response = JsonSerializer.Serialize(reply);
                await writer.WriteLineAsync(response.AsMemory(), token).ConfigureAwait(false);
            }
        }
    }

    private NamedPipeServerStream CreatePipeServer()
    {
        PipeOptions pipeOpts = PipeOptions.Asynchronous | PipeOptions.WriteThrough;
        if (connections.Count == 0)
            pipeOpts |= PipeOptions.FirstPipeInstance;

#if WINDOWS
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner!, PipeAccessRights.FullControl, AccessControlType.Allow));
        ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.Read | PipeAccessRights.Write, AccessControlType.Allow));
        ps.AddAccessRule(new PipeAccessRule("Network", PipeAccessRights.FullControl, AccessControlType.Deny));

        return NamedPipeServerStreamAcl.Create(
            PipeName,
            PipeDirection.InOut,
            MAX_NUMBER_OF_CONNECTION,
            PipeTransmissionMode.Byte,
            pipeOpts,
            0, 0, ps
        );
#endif

        return new NamedPipeServerStream(
            PipeName,
            PipeDirection.InOut,
            MAX_NUMBER_OF_CONNECTION,
            PipeTransmissionMode.Byte,
            pipeOpts
        );
    }

    private static int GetPipeClientPid(SafePipeHandle pipeHandle)
    {
#if WINDOWS
        if (GetNamedPipeClientProcessId(pipeHandle, out uint clientPid))
            return (int)clientPid;

        int errorCode = Marshal.GetLastWin32Error();
        Console.Error.WriteLine($"無法取得pipe client PID。 last error code: 0x{errorCode:X}");
        return -1;
#endif

        // TODO: support other platform
        return -1;
    }

#if WINDOWS
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private extern static bool GetNamedPipeClientProcessId(SafePipeHandle Pipe, out uint ClientProcessId);
#endif

}