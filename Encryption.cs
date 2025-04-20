// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Eggnine.Common;

internal class Encryption : IEncryption, IDisposable
{
    private readonly RandomNumberGenerator _saltProvider = RandomNumberGenerator.Create();
    private readonly SHA256 _hasher = SHA256.Create();
    private bool _disposed;
    private readonly long _iterations;
    private readonly int _saltLength;

    public Encryption(long iterations = 1024*1024, int saltLength = 32)
    {
        _iterations = iterations;
        _saltLength = saltLength;
    }

    public string Encrypt(string toEncrypt)
    {
        CheckForDisposed();
        byte[] salt = new byte[_saltLength];
        _saltProvider.GetBytes(salt, 0, _saltLength);
        string toReturn = CombineHashAndSalt(Hash(toEncrypt, salt, _iterations), salt);
        Clear(salt);
        return toReturn;
    }

    public bool VerifyEncryption(string toVerify, string verifyAgainst)
    {
        CheckForDisposed();
        byte[] salt = GetSalt(verifyAgainst);
        bool verifies = StringComparer.Ordinal.Compare(
            CombineHashAndSalt(Hash(toVerify, salt, _iterations), salt), 
            verifyAgainst) == 0;
        Clear(salt);
        return verifies;
    }

    private string Hash(string toHash, byte[] salt, long iterations)
    {
        string hashed = Convert.ToBase64String(_hasher.ComputeHash(Salt(toHash, salt)));
        for (int i = 0; i < iterations; i++)
        {
            hashed = Convert.ToBase64String(_hasher.ComputeHash(Salt(hashed, salt)));
        }
        return hashed;
    }

    private byte[] Salt(string toSaltStr, byte[] salt)
    {
        byte[] toSalt = Encoding.UTF8.GetBytes(toSaltStr);
        byte[] toReturn = new byte[toSalt.Length];
        for(int i = 0; i < toSalt.Length; i++)
        {
            toReturn[i] = (byte)(toSalt[i] ^ salt[i % _saltLength]);
        }
        Clear(toSalt);
        return toReturn;
    }

    private byte[] GetSalt(string hashAndSalt)
    {
        byte[] salt = new byte[_saltLength];
        Array.Copy(Convert.FromBase64String(hashAndSalt), 0, salt, 0, _saltLength);
        return salt;
    }

    private byte[] GetHash(string hashAndSaltString)
    {
        byte[] hashAndSalt = Convert.FromBase64String(hashAndSaltString);
        byte[] hash = new byte[hashAndSalt.Length - _saltLength];
        if(hashAndSalt.Length < _saltLength)
        {
            throw new InvalidHashAndSaltStringException();
        }
        Array.Copy(hashAndSalt, _saltLength, hash, 0, hashAndSalt.Length - _saltLength);
        Clear(hashAndSalt);
        return hash;
    }

    private string CombineHashAndSalt(string hashString, byte[] salt)
    {
        byte[] hash = Encoding.UTF8.GetBytes(hashString);
        byte[] hashAndSalt = new byte[_saltLength + hash.Length];
        Array.Copy(salt, 0, hashAndSalt, 0, _saltLength);
        Array.Copy(hash, 0, hashAndSalt, _saltLength, hash.Length);
        Clear(hash);
        string toReturn = Convert.ToBase64String(hashAndSalt);
        Clear(hashAndSalt);
        return toReturn;
    }
    
    private void Clear(byte[] toClear)
    {
        _saltProvider.GetBytes(toClear, 0, toClear.Length);
    }

    private void CheckForDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Encryption));
        }
    }

    public void Dispose()
    {
        if(!_disposed)
        {
            _disposed = true;
            _saltProvider.Dispose();
            _hasher.Dispose();
        }
    }
}
