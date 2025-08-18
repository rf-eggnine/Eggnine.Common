// © 2025 Eggnine Syndicate Ltd.
// Versioned PBKDF2-SHA256 hasher suitable for passwords and normalized recovery identifiers.
// Format: $pbkdf2-sha256$v=1$i=<iters>$s=<base64salt>$h=<base64hash>

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Eggnine.Common;

internal sealed class EncryptionV2 : IEncryptionV2, IDisposable
{
    private const string Alg = "pbkdf2-sha256";
    private const string Version = "v=2";
    private const int DefaultIterations = 200_000; // tune in perf tests
    private const int DefaultSaltLen = 16;         // 128-bit
    private const int DefaultHashLen = 32;         // 256-bit

    private readonly int _iterations;
    private readonly int _saltLen;
    private readonly int _hashLen;
    private bool _disposed;

    public EncryptionV2(int iterations = DefaultIterations, int saltLength = DefaultSaltLen, int hashLength = DefaultHashLen)
    {
        _iterations = iterations;
        _saltLen = saltLength;
        _hashLen = hashLength;
    }

    public string Encrypt(string toEncrypt)
    {
        CheckDisposed();

        byte[] salt = RandomNumberGenerator.GetBytes(_saltLen);
        byte[] dk = Rfc2898DeriveBytes.Pbkdf2(toEncrypt, salt, _iterations, HashAlgorithmName.SHA256, _hashLen);

        string sSalt = Convert.ToBase64String(salt);
        string sHash = Convert.ToBase64String(dk);

        // scrub
        CryptographicOperations.ZeroMemory(dk);
        CryptographicOperations.ZeroMemory(salt);

        return $"${Alg}${Version}$i={_iterations}$s={sSalt}$h={sHash}";
    }

    public bool VerifyEncryption(string toVerify, string verifyAgainst)
    {
        CheckDisposed();

        if (!IsTargetFormat(verifyAgainst))
            return false; // not our format (let a compat wrapper handle legacy)

        // $pbkdf2-sha256$v=1$i=...$s=...$h=...
        var parts = verifyAgainst.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5) return false;

        // parts[0]=pbkdf2-sha256, parts[1]=v=1, parts[2]=i=..., parts[3]=s=..., parts[4]=h=...
        if (!parts[1].StartsWith("v=") || !parts[2].StartsWith("i=") || !parts[3].StartsWith("s=") || !parts[4].StartsWith("h="))
            return false;

        if (!int.TryParse(parts[2].AsSpan(2), out int iterations)) return false;

        byte[] salt, expected, actual = Array.Empty<byte>();
        try
        {
            salt = Convert.FromBase64String(parts[3].AsSpan(2).ToString());
            expected = Convert.FromBase64String(parts[4].AsSpan(2).ToString());
        }
        catch
        {
            return false;
        }

        try
        {
            actual = Rfc2898DeriveBytes.Pbkdf2(toVerify, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            bool ok = CryptographicOperations.FixedTimeEquals(actual, expected);
            return ok;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(actual);
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expected);
        }
    }
    public bool IsTargetFormat(string toCheck)
    {
        return toCheck.StartsWith($"${Alg}$", StringComparison.Ordinal);
    }

    public Task<bool> VerifyAndUpgradeAsync(string input, string? stored, Func<string, Task> persistNewHashAsync)
    {
        if (string.IsNullOrEmpty(stored))
        {
            return Task.FromResult(false);
        }
        return Task.FromResult(VerifyEncryption(input, stored));
    }

    public async Task<(bool Verified, bool Upgraded)> TryVerifyAndUpgradeAsync(string input, string? stored, Func<string, Task> persistNewHashAsync)
    {
        return (await VerifyAndUpgradeAsync(input, stored, persistNewHashAsync), false);
    }

    private void CheckDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(EncryptionV2));
    }

    public void Dispose() => _disposed = true;

}
