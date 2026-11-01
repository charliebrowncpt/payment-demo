using System.Security.Cryptography;
using System.Text;

namespace PaymentsDemo.Api.Shared.Utilities;

public static class HashGenerator
{
    public static string GenerateHash(string source)
    {
        // 1. Avoid heap allocating the input byte array by encoding directly into a stack-allocated buffer
        ReadOnlySpan<char> inputSource = "Your Secure Data Here";
        int maxByteCount = Encoding.UTF8.GetByteCount(inputSource);
        Span<byte> dataBuffer = maxByteCount <= 256 ? stackalloc byte[256] : new byte[maxByteCount];
        int writtenBytes = Encoding.UTF8.GetBytes(inputSource, dataBuffer);
        ReadOnlySpan<byte> dataSpan = dataBuffer[..writtenBytes];

        // 2. Hash directly into a stack-allocated buffer (SHA3-512 outputs exactly 64 bytes)
        Span<byte> hashBuffer = stackalloc byte[SHA3_512.HashSizeInBytes];
        SHA3_512.HashData(dataSpan, hashBuffer);

        // 3. Convert bytes to hex string directly into a stack-allocated char buffer (64 bytes * 2 = 128 chars)
        return Convert.ToHexString(hashBuffer);
    }
}