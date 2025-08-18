// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Eggnine.Common;

internal class Encryption : IEncryption, IDisposable
{
    private readonly RandomNumberGenerator _saltProvider = RandomNumberGenerator.Create();
    private readonly ConcurrentQueue<SHA256> _hashers = new();

    private readonly int numberOfHashers = Math.Max(1, Environment.ProcessorCount - 1);
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;
    private readonly long _iterations;
    private readonly int _saltLength;

    public Encryption(long iterations = 1024*512, int saltLength = 32)
    {
        _semaphore = new(numberOfHashers - 1);
        for (int i = 0; i < numberOfHashers; i++)
        {
            _hashers.Enqueue(SHA256.Create());
        }
        _iterations = iterations;
        _saltLength = saltLength;
    }

    public string Encrypt(string toEncrypt)
    {
        CheckForDisposed();
        byte[] salt = new byte[_saltLength];
        _saltProvider.GetBytes(salt, 0, _saltLength);
        string toReturn = CombineHashAndSalt(Hash(toEncrypt, salt, _iterations), salt);
        CryptographicOperations.ZeroMemory(salt);
        return toReturn;
    }

    public bool VerifyEncryption(string toVerify, string verifyAgainst)
    {
        CheckForDisposed();
        byte[] salt = GetSalt(verifyAgainst);
        bool verifies = StringComparer.Ordinal.Compare(
            CombineHashAndSalt(Hash(toVerify, salt, _iterations), salt), 
            verifyAgainst) == 0;
        CryptographicOperations.ZeroMemory(salt);
        return verifies;
    }

    private string Hash(string toHash, byte[] salt, long iterations)
    {
        _semaphore.Wait();
        try
        {
            SHA256? hasher = null;
            while (!_hashers.TryDequeue(out hasher))
            {
                Task.Yield();
            }
            try
                {
                    string hashed = Convert.ToBase64String(hasher.ComputeHash(Salt(toHash, salt)));
                    for (int i = 0; i < iterations; i++)
                    {
                        hashed = Convert.ToBase64String(hasher.ComputeHash(Salt(hashed, salt)));
                    }
                    return hashed;
                }
                finally
                {
                    _hashers.Append(hasher);
                }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private byte[] Salt(string toSaltStr, byte[] salt)
    {
        byte[] toSalt = Encoding.UTF8.GetBytes(toSaltStr);
        byte[] toReturn = new byte[toSalt.Length];
        for(int i = 0; i < toSalt.Length; i++)
        {
            toReturn[i] = (byte)(toSalt[i] ^ salt[i % _saltLength]);
        }
        CryptographicOperations.ZeroMemory(toSalt);
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
        CryptographicOperations.ZeroMemory(hashAndSalt);
        return hash;
    }

    private string CombineHashAndSalt(string hashString, byte[] salt)
    {
        byte[] hash = Encoding.UTF8.GetBytes(hashString);
        byte[] hashAndSalt = new byte[_saltLength + hash.Length];
        Array.Copy(salt, 0, hashAndSalt, 0, _saltLength);
        Array.Copy(hash, 0, hashAndSalt, _saltLength, hash.Length);
        CryptographicOperations.ZeroMemory(hash);
        string toReturn = Convert.ToBase64String(hashAndSalt);
        CryptographicOperations.ZeroMemory(hashAndSalt);
        return toReturn;
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
            while (!_hashers.IsEmpty)
            {
                SHA256 hasher = _hashers.TakeLast(1).Single();
                hasher.Dispose();
            }
        }
    }
}
