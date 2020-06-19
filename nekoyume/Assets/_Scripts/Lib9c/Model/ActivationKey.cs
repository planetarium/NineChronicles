using System;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model
{
    public struct ActivationKey
    {
        public PrivateKey PrivateKey { get; }

        public Address PendingAddress { get; }

        private ActivationKey(PrivateKey privateKey, Address pendingAddress)
        {
            PrivateKey = privateKey;
            PendingAddress = pendingAddress;
        }

        public static (ActivationKey, PendingActivationState) Create(
            PrivateKey privateKey,
            byte[] nonce
        )
        {
            if (privateKey is null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            if (nonce is null)
            {
                throw new ArgumentNullException(nameof(nonce));
            }

            var pendingActivation = new PendingActivationState(nonce, privateKey.PublicKey);
            var activationKey = new ActivationKey(privateKey, pendingActivation.address);

            return (activationKey, pendingActivation);
        }

        public static ActivationKey Decode(string hexWithSlash)
        {
            if (string.IsNullOrEmpty(hexWithSlash) || !hexWithSlash.Contains("/"))
            {
                throw new ArgumentException($"{nameof(hexWithSlash)} seems invalid. [{hexWithSlash}]");
            }

            string[] parts = hexWithSlash.Split('/');
            var privateKey = new PrivateKey(ByteUtil.ParseHex(parts[0]));
            var pendingAddress = new Address(ByteUtil.ParseHex(parts[1]));

            return new ActivationKey(privateKey, pendingAddress);
        }

        public string Encode()
        {
            return $"{ByteUtil.Hex(PrivateKey.ByteArray)}/{ByteUtil.Hex(PendingAddress.ByteArray)}";
        }

        public ActivateAccount CreateActivateAccount(byte[] nonce)
        {
            return new ActivateAccount(PendingAddress, PrivateKey.Sign(nonce));
        }
    }
}
