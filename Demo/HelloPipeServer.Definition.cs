namespace NamedPipePractice.Demo;

public enum HelloAction : int
{
    Hello,
    DoubleHello,
    EchoHello,
    SecureHello,
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