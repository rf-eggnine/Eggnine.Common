// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

namespace Eggnine.Common;

internal sealed class Encryption2Peppered : IEncryptionV2
{
    private const string Alg = "pbkdf2-sha256";
    // Header: $pbkdf2-sha256$v=2$p=1;k=<keyId>$i=<iters>$s=<b64salt>$h=<b64hash>

    private readonly IPepperKeyRing _ring;
    private readonly int _iterations, _saltLen, _hashLen;

    public Encryption2Peppered(IPepperKeyRing ring, int iterations = 200_000, int saltLen = 16, int hashLen = 32)
    { _ring = ring; _iterations = iterations; _saltLen = saltLen; _hashLen = hashLen; }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public string Encrypt(string input)
    {
        byte[]? salt = RandomNumberGenerator.GetBytes(_saltLen);
        string kid = _ring.CurrentKeyId;
        ReadOnlySpan<byte> pepper = _ring.GetPepper(kid);

        byte[] effSalt = HMACSHA256.HashData(pepper, salt);
        if (effSalt.Length != _saltLen) effSalt = effSalt.AsSpan(0, _saltLen).ToArray();

        byte[] dk = Rfc2898DeriveBytes.Pbkdf2(input, effSalt, _iterations, HashAlgorithmName.SHA256, _hashLen);

        string sSalt = Convert.ToBase64String(salt);
        string sHash = Convert.ToBase64String(dk);

        CryptographicOperations.ZeroMemory(dk);
        CryptographicOperations.ZeroMemory(effSalt);
        CryptographicOperations.ZeroMemory(salt);

        return $"${Alg}$v=2$p=1;k={kid}$i={_iterations}$s={sSalt}$h={sHash}";
    }

    public bool VerifyEncryption(string input, string encoded)
    {
        if (!IsTargetFormat(encoded)) return false;

        var parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        // v=?, maybe p=1;k=KID, i=..., s=..., h=...
        int idx = 1;
        string v = parts[idx++]; // v=1|2
        bool peppered = false;
        string? kid = null;

        if (parts[idx].StartsWith("p="))
        {
            peppered = parts[idx++] == "p=1";
            if (peppered && parts[idx].StartsWith("k="))
                kid = parts[idx++].Substring(2);
        }

        int iterations = int.Parse(parts[idx++].AsSpan(2)); // i=
        var salt = Convert.FromBase64String(parts[idx++].AsSpan(2).ToString()); // s=
        var expected = Convert.FromBase64String(parts[idx].AsSpan(2).ToString()); // h=

        byte[] effSalt;
        if (peppered && !string.IsNullOrWhiteSpace(kid))
        {
            var pepper = _ring.GetPepper(kid);
            effSalt = HMACSHA256.HashData(pepper, salt);
            if (effSalt.Length != salt.Length) effSalt = effSalt.AsSpan(0, salt.Length).ToArray();
        }
        else
        {
            effSalt = salt; // supports v=1 (no pepper) during migration
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(input, effSalt, iterations, HashAlgorithmName.SHA256, expected.Length);
        bool ok = CryptographicOperations.FixedTimeEquals(actual, expected);

        CryptographicOperations.ZeroMemory(actual);
        CryptographicOperations.ZeroMemory(expected);
        CryptographicOperations.ZeroMemory(effSalt);
        CryptographicOperations.ZeroMemory(salt);
        return ok;
    }

    public bool IsTargetFormat(string toVerify)
    {
        return toVerify.StartsWith($"${Alg}$", StringComparison.Ordinal);
    }
}