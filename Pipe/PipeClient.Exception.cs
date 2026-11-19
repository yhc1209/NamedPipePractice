using System;

namespace NamedPipePractice.Pipe;

public class PipeClientException : Exception
{
    public PipeClientExcpCode Code { get; init; }

    public PipeClientException(PipeClientExcpCode code) : base(code.ToString())
    {
        Code = code;
    }

    public PipeClientException(PipeClientExcpCode code, Exception? innerException) : base(code.ToString(), innerException)
    {
        Code = code;
    }
}

public enum PipeClientExcpCode
{
    NoReplyData,
    NoReplyPackage,
}
