using System.Text.Json;
using NamedPipePractice.Pipe;

namespace NamedPipePractice.Demo;

public static class PackageExtension
{
    public static HelloInfo? GetHelloInfo(this QueryPackage self)
    {
        if (self.Data is null)
            return null;
        string json = JsonSerializer.Serialize(self.Data);
        return JsonSerializer.Deserialize<HelloInfo>(json);
    }

    public static void ValidateAuthorization(this QueryPackage self)
    {
        if (string.IsNullOrWhiteSpace(self.Token))
            throw new PipeServerException(PipeServerExcpCode.BadAuth);
        
        // TODO: validate token
    }
}