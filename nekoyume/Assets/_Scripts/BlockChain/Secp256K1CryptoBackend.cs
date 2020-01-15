using System.Security.Cryptography;
using Libplanet;
using Secp256k1Net;
using Libplanet.Crypto;

namespace Nekoyume.BlockChain
{
    public class Secp256K1CryptoBackend : ICryptoBackend
    {
        private readonly Secp256k1 _instance = new Secp256k1();

        public bool Verify(
            HashDigest<SHA256> messageHash,
            byte[] signature,
            PublicKey publicKey)
        {
            var secp256K1Signature = new byte[64];
            _instance.SignatureParseDer(secp256K1Signature, signature);

            byte[] secp256K1PublicKey = new byte[64];
            byte[] serializedPublicKey = publicKey.Format(false);
            _instance.PublicKeyParse(secp256K1PublicKey, serializedPublicKey);

            return _instance.Verify(secp256K1Signature, messageHash.ToByteArray(), secp256K1PublicKey);
        }
    }
}
