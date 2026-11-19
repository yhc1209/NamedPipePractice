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
            Console.WriteLine("\n[系統] 收到中斷信號 (Ctrl+C)，正在結束程式...");
        };

        // 1. 啟動 HelloPipeServer
        var server = new HelloPipeServer();
        server.StartListen(cts.Token);
        Console.WriteLine($"[Server] 已啟動 Named Pipe 伺服器，監聽服務: '{HelloPipeServer.ServiceName}'");

        // 2. 建立 HelloPipeClient
        var client = new HelloPipeClient();

        Console.WriteLine("==========================================================");
        Console.WriteLine("  HelloPipe 示範程式已就緒");
        Console.WriteLine("  - 直接輸入文字並按 Enter 即發送訊息");
        Console.WriteLine("  - 輸入 /mode <1-4> 切換動作 (1: Hello, 2: DoubleHello, 3: Echo, 4: Secure)");
        Console.WriteLine("  - 輸入 /sender <名稱> 變更發送者名稱");
        Console.WriteLine("  - 按下 Ctrl+C 可隨時結束程式");
        Console.WriteLine("==========================================================");

        var currentAction = HelloAction.EchoHello;
        string senderName = "User";

        while (!cts.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.Write($"[{senderName} | 模式: {currentAction}] 請輸入訊息: ");

            string? input;
            try
            {
                input = await ReadLineWithCancellationAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (input == null || cts.IsCancellationRequested)
            {
                break;
            }

            input = input.Trim();
            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            // 指令處理
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

            // 發送訊息給 HelloPipeServer
            var info = new HelloInfo
            {
                Sender = senderName,
                Greeting = input
            };

            try
            {
                var reply = await client.SendHelloAsync(currentAction, info, cts.Token);
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
                Console.WriteLine($"[錯誤] 請求逾時: {excp.Message}");
                Console.ResetColor();
            }
            catch (Exception excp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[錯誤] 發送失敗: {excp.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("[系統] 程式已成功關閉。");
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


