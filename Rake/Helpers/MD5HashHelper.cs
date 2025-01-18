using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Rake.Helpers;

// ReSharper disable once InconsistentNaming
public static class MD5HashHelper
{
    [ThreadStatic]
    private static MD5? _threadInstance;
    private static MD5 HashAlgorithm => _threadInstance ??= MD5.Create();

    // ReSharper disable once InconsistentNaming
    public static string GetMD5Hash<T>(this T value) => ComputeHash(value);

    public static Guid GetGuidHash<T>(this T value) => new(value.GetMD5Hash());

    public static string ComputeHash<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        byte[] bytes;
        switch (value)
        {
            case string str:
                bytes = Encoding.UTF8.GetBytes(str);
                break;
            case int intValue:
                bytes = BitConverter.GetBytes(intValue);
                break;
            case long longValue:
                bytes = BitConverter.GetBytes(longValue);
                break;
            case double doubleValue:
                bytes = BitConverter.GetBytes(doubleValue);
                break;
            case float floatValue:
                bytes = BitConverter.GetBytes(floatValue);
                break;
            case Stream stream:
            {
                bytes = ComputeStreamHashCore(stream);
                break;
            }
            default:
                var jsonString = JsonSerializer.Serialize(
                    value,
                    typeof(T),
                    GlobalJsonSerializerContext.Default
                );
                bytes = Encoding.UTF8.GetBytes(jsonString);
                break;
        }

        return ComputeHashCore(bytes);
    }

    private static byte[] ComputeStreamHashCore(Stream stream)
    {
        using var buffer = MemoryPool<byte>.Shared.Rent(4096);
        using var memoryStream = new MemoryStream();
        int bytesRead;
        while ((bytesRead = stream.Read(buffer.Memory.Span)) > 0)
        {
            memoryStream.Write(buffer.Memory.Span[..bytesRead]);
        }

        return memoryStream.ToArray();
    }

    private static unsafe string ComputeHashCore(ReadOnlySpan<byte> bytes)
    {
        Span<byte> hashBytes = stackalloc byte[16];
        HashAlgorithm.TryComputeHash(bytes, hashBytes, out _);

        const int charArrayLength = 32;
        var charArrayPtr = stackalloc char[charArrayLength];

        var charPtr = charArrayPtr;
        for (var i = 0; i < 16; i++)
        {
            var hashByte = hashBytes[i];
            *charPtr++ = GetHexValue(hashByte >> 4);
            *charPtr++ = GetHexValue(hashByte & 0xF);
        }

        return new string(charArrayPtr, 0, charArrayLength);
    }

    //Based on byte conversion implementation in BitConverter (but with the dash stripped)
    //https://github.com/dotnet/coreclr/blob/fbc11ea6afdaa2fe7b9377446d6bb0bd447d5cb5/src/mscorlib/shared/System/BitConverter.cs#L409-L440
    private static char GetHexValue(int i)
    {
        if (i < 10)
            return (char)(i + '0');

        return (char)(i - 10 + 'A');
    }
}
