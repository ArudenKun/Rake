using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace Rake.Services.Caching;

public class FileCacheSerializer(JsonSerializerOptions jsonSerializerOptions)
    : IFusionCacheSerializer
{
    public byte[] Serialize<T>(T? obj)
    {
        var typeInfo = jsonSerializerOptions.GetTypeInfo(typeof(T));
        return JsonSerializer.SerializeToUtf8Bytes(obj, typeInfo);
    }

    public T? Deserialize<T>(byte[] data)
    {
        var typeInfo = jsonSerializerOptions.GetTypeInfo(typeof(T));
        return (T?)JsonSerializer.Deserialize(data, typeInfo);
    }

    public async ValueTask<byte[]> SerializeAsync<T>(T? obj)
    {
        return await Task.Run(() => Serialize(obj));
    }

    public async ValueTask<T?> DeserializeAsync<T>(byte[] data)
    {
        var typeInfo = jsonSerializerOptions.GetTypeInfo(typeof(T));
        using var stream = new MemoryStream(data);
        return (T?)await JsonSerializer.DeserializeAsync(stream, typeInfo);
    }
}
