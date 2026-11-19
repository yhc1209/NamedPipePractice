using System.Text.Json.Serialization;

namespace NamedPipePractice.Pipe;

/// <summary>請求封包。</summary>
public class QueryPackage
{
    public required int Action { get; set; }
    public required object? Data { get; set; }
}

/// <summary>回覆封包。</summary>
public class ReplyPackage
{
    public required int Action { get; set; }
    public required int Code { get; set; }
    public required string Message { get; set; }
    public required object? Data { get; set; }
}

/// <summary>處理請求、傳出回覆的處理器。</summary>
/// <param name="query"></param>
/// <returns></returns>
public delegate ReplyPackage PackageHandler(QueryPackage query);