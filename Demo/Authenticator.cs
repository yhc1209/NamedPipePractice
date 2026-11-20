namespace NamedPipePractice.Demo;

/// <summary>驗證器。</summary>
/// <remarks>[!] demo用的，無安全性。</remarks>
public class Authenticator
{
    private const string PASSWORD = "P@ssw0rd";

    /// <summary>進行密碼驗證。</summary>
    /// <param name="input">輸入的密碼。</param>
    /// <returns>回傳密碼是否正確。</returns>
    /// <remarks>[!] demo用的，無安全性。</remarks>
    public static bool Authenticate(string input)
    {
        return PASSWORD.Equals(input);
    }

}

public class RegisterQueryInfo
{
    public required int ProcessId { get; init; }
    public required string MainModulePath { get; init; }
}

public class RegisterReplyInfo
{
    public required string Key { get; init; }
}