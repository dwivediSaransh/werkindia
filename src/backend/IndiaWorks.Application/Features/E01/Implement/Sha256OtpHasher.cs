using System.Security.Cryptography;
using System.Text;

namespace IndiaWorks.Application.Features.E01.Implement;

public sealed class Sha256OtpHasher : IOtpHasher
{
    public string Hash(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(hash);
    }

    public bool Verify(string code, string hashedCode)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(hashedCode))
        {
            return false;
        }

        var computedHash = Hash(code);
        return string.Equals(computedHash, hashedCode, StringComparison.Ordinal);
    }
}
