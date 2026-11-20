using System;
using System.Diagnostics;
using System.Security.Cryptography;
using NamedPipePractice.Pipe;

namespace NamedPipePractice.Demo;

public class HelloPipeServer : PipeServer
{
    public const string ServiceName = "HelloServer";
    private readonly byte[] TokenHashKey = new byte[HelloToken.HASH_KEY_LENGTH];

    public HelloPipeServer() : base(ServiceName)
    {
        RandomNumberGenerator.Fill(TokenHashKey);
        SetHandler(HandleQuery);
    }

    private ReplyPackage HandleQuery(int pid, QueryPackage query)
    {
        HelloAction action = (HelloAction)query.Action;
        Console.WriteLine($"[Server] 收到請求: Action={action}, Token={query.Token ?? "(none)"}");

        ReplyPackage reply;
        switch (action)
        {
            case HelloAction.Register:
                reply = GetRegisterReply(pid, query.GetData<RegisterQueryInfo>());
                break;
            case HelloAction.Hello:
            case HelloAction.DoubleHello:
            case HelloAction.EchoHello:
                reply = GetHelloReply(action, query.GetData<HelloInfo>());
                break;
            case HelloAction.SecureHello:
                if (ValidateToken(pid, query.Token))
                    reply = CreateOkReply(action, "Hello, my secure friend.");
                else
                    reply = CreateFailureReply(action, "permission deny!");
                break;
            default:
                reply = CreateFailureReply(action, "...");
                break;
        }

        Console.WriteLine($"[Server] 回覆結果: Code={(HelloReplyCode)reply.Code}, Message={reply.Message}");
        return reply;
    }

    private ReplyPackage GetRegisterReply(int pid, RegisterQueryInfo? info)
    {
        if (pid < 0)
            return CreateFailureReply(HelloAction.Register, "Invalid PID.");
        if (info is null)
            return CreateFailureReply(HelloAction.Register, "No register info.");
        if (pid != info.ProcessId)
            return CreateFailureReply(HelloAction.Register, "Bad request.");

        using (Process p = Process.GetProcessById(pid))
        {
            if (p.MainModule?.FileName == info.MainModulePath)
            {
                // TODO: 確認path是否在白名單

                var replyInfo = new RegisterReplyInfo()
                {
                    Key = Convert.ToBase64String(TokenHashKey)
                };
                return new ReplyPackage
                {
                    Action = (int)HelloAction.Register,
                    Code = (int)HelloReplyCode.OK,
                    Message = "Successful.",
                    Data = replyInfo,
                };
            }
        }

        return CreateFailureReply(HelloAction.Register, "Bad request.");
    }

    private bool ValidateToken(int pid, string? tokenString)
    {
        try
        {
            HelloToken token = HelloToken.ParseToken(TokenHashKey, tokenString);
            token.CheckExpiration();
            if (token.AudiencePid != pid)
                throw new InvalidOperationException($"Unexcepted token owner! (pid: {pid})");

            return true;
        }
        catch (Exception excp)
        {
            Console.Error.WriteLine(
                "Bad Token: {0} - {1}",
                excp.GetType(),
                excp.Message
            );
        }
        return false;
    }

    private ReplyPackage GetHelloReply(HelloAction action, HelloInfo? info)
    {
        if (info is null)
            return CreateOkReply(action, "Hello, who are you?");

        string replyMsg = "Hello, world";
        switch (action)
        {
            case HelloAction.Hello:
                replyMsg = $"Hello, {info.Sender}.";
                break;
            case HelloAction.DoubleHello:
                replyMsg = string.Format("Hello, {0}. Hello, {0}", info.Sender);
                break;
            case HelloAction.EchoHello:
                replyMsg = info.Greeting;
                break;
        }
        return CreateOkReply(action, replyMsg);

    }

    private ReplyPackage CreateOkReply(HelloAction action, string msg)
    {
        return new ReplyPackage
        {
            Action = (int)action,
            Code = (int)HelloReplyCode.OK,
            Message = msg,
            Data = null,
        };
    }

    private static ReplyPackage CreateFailureReply(HelloAction action, string msg)
    {
        return new ReplyPackage
        {
            Action = (int)action,
            Code = (int)HelloReplyCode.Failure,
            Message = msg,
            Data = null,
        };
    }
}