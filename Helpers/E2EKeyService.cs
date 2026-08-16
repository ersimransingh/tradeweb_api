using System;
using System.Security.Cryptography;

namespace TradeWeb.API.Helpers
{
    /// <summary>
    /// Manages the server-side P-256 ECDH key pair with hourly rotation.
    /// Registered as a singleton; <see cref="GetSnapshot"/> is thread-safe and returns a
    /// consistent (Parameters, PublicKey) pair captured under a lock, so request decryption
    /// and response header always reference the same key generation.
    /// </summary>
    public class E2EKeyService
    {
        private static readonly TimeSpan KeyLifetime = TimeSpan.FromHours(1);

        private readonly object _lock = new object();
        private ECParameters _parameters;
        private byte[] _publicKey;
        private DateTime _keyGeneratedAt;

        public E2EKeyService()
        {
            GenerateKey();
        }

        /// <summary>
        /// Returns a consistent snapshot of the current (Parameters, PublicKey).
        /// Rotates the key if it has exceeded <see cref="KeyLifetime"/>.
        /// </summary>
        public (ECParameters Parameters, byte[] PublicKey) GetSnapshot()
        {
            lock (_lock)
            {
                if (DateTime.UtcNow - _keyGeneratedAt > KeyLifetime)
                    GenerateKey();

                return (_parameters, _publicKey);
            }
        }

        /// <summary>
        /// Convenience accessor for the current public key (e.g. the E2EPublicKey endpoint).
        /// Rotates the key if stale.
        /// </summary>
        public byte[] PublicKey
        {
            get
            {
                lock (_lock)
                {
                    if (DateTime.UtcNow - _keyGeneratedAt > KeyLifetime)
                        GenerateKey();

                    return _publicKey;
                }
            }
        }

        // Must be called with _lock held.
        private void GenerateKey()
        {
            using (var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
            {
                _parameters = ecdh.ExportParameters(true);
                var pk = new byte[65];
                pk[0] = 0x04;
                Buffer.BlockCopy(_parameters.Q.X, 0, pk, 1, 32);
                Buffer.BlockCopy(_parameters.Q.Y, 0, pk, 33, 32);
                _publicKey = pk;
                _keyGeneratedAt = DateTime.UtcNow;
            }
        }
    }
}
