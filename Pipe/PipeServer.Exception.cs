using System;

namespace NamedPipePractice.Pipe;

public class PipeServerException : Exception
{
    public PipeServerExcpCode Code { get; init; }

    public PipeServerException(PipeServerExcpCode code) : base(code.ToString())
    {
        Code = code;
    }
}

public enum PipeServerExcpCode
{
    NoQueryData,
    NoQueryPackage,
    NoQueryHandler,
    BadAuth,
}