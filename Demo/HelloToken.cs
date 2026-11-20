using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;

namespace NamedPipePractice.Demo;

public class HelloToken
{
    [JsonIgnore]
    public const int HASH_KEY_LENGTH = 32;
    [JsonIgnore]
    public const int TOKEN_EXPIRATION_SEC = 30;

    [JsonPropertyName("iss")]
    public required string Issuer { get; init; }
    [JsonPropertyName("aud")]
    public required string Audience { get; init; }
    [JsonPropertyName("iat")]
    public required DateTime IssuedAt { get; init; }
    [JsonPropertyName("exp")]
    public required DateTime ExpirationTime { get; init; }
    [JsonPropertyName("pid")]
    public required int AudiencePid { get; init; }
    [JsonPropertyName("tid")]
    public required int Nonce { get; init; }

    public HelloToken(string iss, string aud, int pid)
    {
        Issuer = iss;
        Audience = aud;
        AudiencePid = pid;
        IssuedAt = DateTime.UtcNow;
        ExpirationTime = IssuedAt.AddSeconds(TOKEN_EXPIRATION_SEC);
        Nonce = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
    }

    public void CheckExpiration()
    {
        DateTime now = DateTime.UtcNow;
        if (now > ExpirationTime || now < IssuedAt)
            throw new HelloTokenException("The token is invalid.");
    }

    public string ComputeToken(byte[] key)
    {
        if (key is null)
            throw new ArgumentNullException("Invalid key to compute token.");
        if (key.Length != HASH_KEY_LENGTH)
            throw new ArgumentException("Invalid key to compute token.");

        byte[] payloadData = JsonSerializer.SerializeToUtf8Bytes(this);
        byte[] hashData = HMACSHA256.HashData(key, payloadData);
        return string.Format(
            "{0}.{1}",
            Convert.ToBase64String(payloadData),
            Convert.ToBase64String(hashData)
        );
    }

    public static HelloToken ParseToken(byte[] key, string? token)
    {
        if (key is null)
            throw new ArgumentNullException("Invalid key to parse token.");
        if (key.Length != HASH_KEY_LENGTH)
            throw new ArgumentException("Invalid key to parse token.");

        ArgumentException.ThrowIfNullOrEmpty(token, nameof(token));
        string[] info = token.Split(".", StringSplitOptions.None);
        if (info.Length != 2)
            throw new HelloTokenException("Bad format of the input token.");

        byte[] payload = Convert.FromBase64String(info[0]);
        byte[] hash = Convert.FromBase64String(info[1]);
        byte[] computedHash = HMACSHA256.HashData(key, payload);
        if (!hash.SequenceEqual(computedHash))
            throw new HelloTokenException("Bad integrity of the input token.");

        return JsonSerializer.Deserialize<HelloToken>(payload) ??
            throw new HelloTokenException("Cannot parse the input token.");
    }
}

public class HelloTokenException : Exception
{
    public HelloTokenException(string msg) : base(msg) { }
}
