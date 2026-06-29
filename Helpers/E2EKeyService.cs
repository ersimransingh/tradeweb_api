using System;
using System.Security.Cryptography;

namespace TradeWeb.API.Helpers
{
    public class E2EKeyService
    {
        public ECParameters Parameters { get; }
        public byte[] PublicKey { get; }

        public E2EKeyService()
        {
            using (var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
            {
                Parameters = ecdh.ExportParameters(true);
                PublicKey = new byte[65];
                PublicKey[0] = 0x04;
                Buffer.BlockCopy(Parameters.Q.X, 0, PublicKey, 1, 32);
                Buffer.BlockCopy(Parameters.Q.Y, 0, PublicKey, 33, 32);
            }
        }
    }
}
