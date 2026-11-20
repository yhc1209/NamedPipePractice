namespace NamedPipePractice.Demo;

public enum HelloAction : int
{
    Hello,
    DoubleHello,
    EchoHello,
    SecureHello,

    // authentication
    Register    = 10000
}

public enum HelloReplyCode : int
{
    OK,
    Failure,
}

public class HelloInfo
{
    public required string Sender { get; init; }
    public required string Greeting { get; init; }
}
