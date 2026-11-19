using NamedPipePractice.Pipe;

namespace NamedPipePractice.Demo;

public class HelloPipeServer : PipeServer
{
    public const string ServiceName = "HelloServer";

    public HelloPipeServer() : base(ServiceName)
    {
        SetHandler(HandleQuery);
    }

    private ReplyPackage HandleQuery(QueryPackage query)
    {
        HelloAction action = (HelloAction)query.Action;
        switch (action)
        {
            case HelloAction.Hello:
            case HelloAction.DoubleHello:
            case HelloAction.EchoHello:
                return GetHelloReply(action, query.GetHelloInfo());
            case HelloAction.SecureHello:
            default:
                return CreateFailureReply(action, "...");
        }
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