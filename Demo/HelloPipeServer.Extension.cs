using System.Text.Json;
using NamedPipePractice.Pipe;

namespace NamedPipePractice.Demo;

public static class PackageExtension
{
    public static T? GetData<T>(this QueryPackage self)
    {
        if (self.Data is null)
            return default(T);
        string json = JsonSerializer.Serialize(self.Data);
        return JsonSerializer.Deserialize<T>(json);
    }
}