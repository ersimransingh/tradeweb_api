using System;
using System.Security.Cryptography;
using System.Text;

namespace TradeWeb.API.Helpers
{
    public static class E2EEncryptionHelper
    {
        public const string PublicKeyHeader = "x-e2e-public-key";
        private const int NonceLength = 12;
        private const int TagLength = 16;

        /// <summary>
        /// Generates a fresh ephemeral P-256 ECDH key pair.
        /// Returns the 32-byte private key; outputs the 65-byte uncompressed public key (0x04 || X || Y).
        /// </summary>
        public static byte[] GenerateKeyPair(out byte[] publicKey)
        {
            using (var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
            {
                var parameters = ecdh.ExportParameters(false);
                publicKey = new byte[65];
                publicKey[0] = 0x04;
                Buffer.BlockCopy(parameters.Q.X, 0, publicKey, 1, 32);
                Buffer.BlockCopy(parameters.Q.Y, 0, publicKey, 33, 32);

                var privateParameters = ecdh.ExportParameters(true);
                return privateParameters.D;
            }
        }

        /// <summary>
        /// Derives the shared secret using ECDH with our private key and the peer's uncompressed public key.
        /// </summary>
        public static byte[] DeriveSharedSecret(ECParameters serverParameters, byte[] otherPublicKey)
        {
            if (otherPublicKey == null || otherPublicKey.Length != 65 || otherPublicKey[0] != 0x04)
                throw new ArgumentException("Peer public key must be 65-byte uncompressed SEC1 format.");

            using (var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
            {
                var parameters = new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    D = serverParameters.D,
                    Q = new ECPoint
                    {
                        X = (byte[])serverParameters.Q.X.Clone(),
                        Y = (byte[])serverParameters.Q.Y.Clone()
                    }
                };
                ecdh.ImportParameters(parameters);

                using (var otherEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
                {
                    var otherParams = new ECParameters
                    {
                        Curve = ECCurve.NamedCurves.nistP256,
                        Q = new ECPoint
                        {
                            X = new byte[32],
                            Y = new byte[32]
                        }
                    };
                    Buffer.BlockCopy(otherPublicKey, 1, otherParams.Q.X, 0, 32);
                    Buffer.BlockCopy(otherPublicKey, 33, otherParams.Q.Y, 0, 32);
                    otherEcdh.ImportParameters(otherParams);

                    return ecdh.DeriveKeyMaterial(otherEcdh.PublicKey);
                }
            }
        }

        /// <summary>
        /// Derives a 256-bit AES key from the ECDH shared secret.
        /// On .NET Core 3.1/Linux, DeriveKeyMaterial already returns a 32-byte value
        /// (SHA-256 of the raw shared x-coordinate), so we return it directly.
        /// </summary>
        public static byte[] DeriveAesKey(byte[] sharedSecret)
        {
            if (sharedSecret == null || sharedSecret.Length == 0)
                throw new ArgumentException("Shared secret cannot be null or empty.");

            // For P-256 the shared secret material is already 32 bytes.
            if (sharedSecret.Length == 32)
                return sharedSecret;

            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(sharedSecret);
            }
        }

        /// <summary>
        /// Encrypts plaintext with AES-256-GCM.
        /// Returns base64(nonce || tag || ciphertext).
        /// </summary>
        public static string Encrypt(byte[] aesKey, string plainText)
        {
            var nonce = new byte[NonceLength];
            RandomNumberGenerator.Fill(nonce);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagLength];

            using (var aes = new AesGcm(aesKey))
            {
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            var result = new byte[NonceLength + TagLength + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceLength);
            Buffer.BlockCopy(tag, 0, result, NonceLength, TagLength);
            Buffer.BlockCopy(cipherBytes, 0, result, NonceLength + TagLength, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts base64(nonce || tag || ciphertext) with AES-256-GCM.
        /// </summary>
        public static string Decrypt(byte[] aesKey, string cipherTextBase64)
        {
            var cipherBytes = Convert.FromBase64String(cipherTextBase64);
            if (cipherBytes.Length < NonceLength + TagLength)
                throw new ArgumentException("Invalid ciphertext length.");

            var nonce = new byte[NonceLength];
            var tag = new byte[TagLength];
            var cipher = new byte[cipherBytes.Length - NonceLength - TagLength];

            Buffer.BlockCopy(cipherBytes, 0, nonce, 0, NonceLength);
            Buffer.BlockCopy(cipherBytes, NonceLength, tag, 0, TagLength);
            Buffer.BlockCopy(cipherBytes, NonceLength + TagLength, cipher, 0, cipher.Length);

            var plainBytes = new byte[cipher.Length];
            using (var aes = new AesGcm(aesKey))
            {
                aes.Decrypt(nonce, cipher, tag, plainBytes);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
