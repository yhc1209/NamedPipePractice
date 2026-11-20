using System;
using System.Threading;
using System.Threading.Tasks;
using NamedPipePractice.Demo;
using NamedPipePractice.Pipe;

namespace NamedPipePractice;

class Program
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\n[系統] 收到中斷信號 (Ctrl+C)，正在結束...");
        };

        string? mode = null;
        if (args.Length > 0)
        {
            mode = args[0].Trim().ToLowerInvariant();
        }

        if (string.IsNullOrEmpty(mode))
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine($"  Named Pipe Demo (Process ID: {Environment.ProcessId})");
            Console.WriteLine("  請選擇此處理程序要執行的角色：");
            Console.WriteLine("    [1] Server (伺服器端)");
            Console.WriteLine("    [2] Client (客戶端)");
            Console.WriteLine("==========================================================");
            Console.Write("請輸入選項 [1/2] (按 Enter 預設為 1): ");

            string? choice = Console.ReadLine()?.Trim();
            if (choice == "2" || choice?.Equals("client", StringComparison.OrdinalIgnoreCase) == true)
            {
                mode = "client";
            }
            else
            {
                mode = "server";
            }
        }

        if (mode is "server" or "-s" or "--server" or "1")
        {
            await RunServerAsync(cts.Token);
        }
        else if (mode is "client" or "-c" or "--client" or "2")
        {
            await RunClientAsync(cts.Token);
        }
        else
        {
            Console.WriteLine($"[錯誤] 未知的模式: '{mode}'");
            Console.WriteLine("使用方式:");
            Console.WriteLine("  dotnet run -- server    # 啟動伺服端 Process");
            Console.WriteLine("  dotnet run -- client    # 啟動客戶端 Process");
        }
    }

    private static async Task RunServerAsync(CancellationToken token)
    {
        Console.Title = $"NamedPipe Server (PID: {Environment.ProcessId})";

        var server = new HelloPipeServer();
        server.StartListen(token);

        Console.WriteLine("==========================================================");
        Console.WriteLine($"  [Server Process] (PID: {Environment.ProcessId})");
        Console.WriteLine($"  - 監聽服務名稱: '{HelloPipeServer.ServiceName}'");
        Console.WriteLine($"  - 最大連線數: {PipeServer.MAX_NUMBER_OF_CONNECTION}");
        Console.WriteLine("  - 伺服端正在等候 Client 連線與發送請求...");
        Console.WriteLine("  - 按下 Ctrl+C 可停止伺服器");
        Console.WriteLine("==========================================================");

        var tcs = new TaskCompletionSource();
        using (token.Register(() => tcs.TrySetResult()))
        {
            await tcs.Task;
        }

        Console.WriteLine("[Server] 伺服器已正常停止。");
    }

    private static async Task RunClientAsync(CancellationToken token)
    {
        Console.Title = $"NamedPipe Client (PID: {Environment.ProcessId})";

        var client = new HelloPipeClient();

        Console.WriteLine("==========================================================");
        Console.WriteLine($"  [Client Process] (PID: {Environment.ProcessId})");
        Console.WriteLine($"  - 目標服務: '{HelloPipeServer.ServiceName}'");
        Console.WriteLine("  - 直接輸入文字並按 Enter 即發送訊息");
        Console.WriteLine("  - 輸入 /mode <1-4> 切換動作 (1: Hello, 2: DoubleHello, 3: Echo, 4: Secure)");
        Console.WriteLine("  - 輸入 /sender <名稱> 變更發送者名稱");
        Console.WriteLine("  - 輸入 /token <值> 設定驗證 Token (例如: /token P@ssw0rd)");
        Console.WriteLine("  - 按下 Ctrl+C 可隨時結束程式");
        Console.WriteLine("==========================================================");

        var currentAction = HelloAction.EchoHello;
        string senderName = "User";

        while (!token.IsCancellationRequested)
        {
            Console.WriteLine();
            string tokenPrompt = string.IsNullOrEmpty(client.AuthToken) ? "無" : client.AuthToken;
            Console.Write($"[{senderName} | 模式: {currentAction} | Token: {tokenPrompt}] 請輸入訊息: ");

            string? input;
            try
            {
                input = await ReadLineWithCancellationAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (input == null || token.IsCancellationRequested)
            {
                break;
            }

            input = input.Trim();
            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            // 指令處理: 切換模式
            if (input.StartsWith("/mode", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && int.TryParse(parts[1], out int modeVal) && Enum.IsDefined(typeof(HelloAction), modeVal - 1))
                {
                    currentAction = (HelloAction)(modeVal - 1);
                    Console.WriteLine($"[系統] 已切換模式為: {currentAction}");
                }
                else
                {
                    Console.WriteLine("[系統] 無效的模式！可用模式: 1: Hello, 2: DoubleHello, 3: EchoHello, 4: SecureHello");
                }
                continue;
            }

            // 指令處理: 變更發送者
            if (input.StartsWith("/sender", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    senderName = parts[1].Trim();
                    Console.WriteLine($"[系統] 發送者名稱已變更為: {senderName}");
                }
                else
                {
                    Console.WriteLine("[系統] 請提供發送者名稱，例如: /sender Alice");
                }
                continue;
            }

            // 指令處理: 設定 Token
            if (input.StartsWith("/token", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    string newToken = parts[1].Trim();
                    if (newToken.Equals("clear", StringComparison.OrdinalIgnoreCase) || newToken.Equals("none", StringComparison.OrdinalIgnoreCase))
                    {
                        client.AuthToken = null;
                        Console.WriteLine("[系統] 已清除 Token。");
                    }
                    else
                    {
                        client.AuthToken = newToken;
                        Console.WriteLine($"[系統] Token 已設定為: {client.AuthToken}");
                    }
                }
                else
                {
                    Console.WriteLine($"[系統] 目前 Token: {client.AuthToken ?? "(無)"}。可輸入 '/token <字串>' 設定或 '/token clear' 清除。");
                }
                continue;
            }

            // 發送訊息給 HelloPipeServer
            var info = new HelloInfo
            {
                Sender = senderName,
                Greeting = input
            };

            try
            {
                var reply = await client.SendHelloAsync(currentAction, info, client.AuthToken, token);
                var replyCode = (HelloReplyCode)reply.Code;
                Console.WriteLine($"[Client 收到回覆] 狀態: {replyCode} ({reply.Code}) | 訊息: {reply.Message}");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (TimeoutException excp)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[逾時] 連線伺服器逾時，請確認 Server 處理程序是否已啟動。({excp.Message})");
                Console.ResetColor();
            }
            catch (PipeClientException excp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[錯誤] 管道通訊錯誤: {excp.Code}");
                Console.ResetColor();
            }
            catch (Exception excp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[錯誤] 發送失敗: {excp.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("[Client] 客戶端已正常關閉。");
    }

    private static async Task<string?> ReadLineWithCancellationAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    return Console.ReadLine();
                }
                await Task.Delay(50, token).ConfigureAwait(false);
            }
        }
        catch (InvalidOperationException)
        {
            // 輸入被重新導向時不支援 KeyAvailable，改用 Task.Run 呼叫同步 ReadLine
            var readLineTask = Task.Run(() => Console.ReadLine(), token);
            var completedTask = await Task.WhenAny(readLineTask, Task.Delay(-1, token)).ConfigureAwait(false);
            if (completedTask == readLineTask)
            {
                return await readLineTask.ConfigureAwait(false);
            }
        }

        return null;
    }
}



