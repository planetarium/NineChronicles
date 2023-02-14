using System;
using System.Text;
using System.Text.Json;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game.Character;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Secp256k1Net;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Lobby : MonoBehaviour
    {
        [SerializeField]
        private LobbyCharacter character;

        [SerializeField]
        private FriendCharacter friendCharacter;

        // [SerializeField]
        // private Avatar.Avatar avatar;

        public LobbyCharacter Character => character;

        public FriendCharacter FriendCharacter => friendCharacter;
        // public Avatar.Avatar Avatar => avatar;

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                var url = $"{Game.instance.URL.DccMetadata}{111}.json";
                StartCoroutine(RequestManager.instance.GetJson(url, (json) =>
                {
                    var test = JsonSerializer.Deserialize<DccMetadata>(json);
                    Debug.Log("done");
                }));
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                StartCoroutine(RequestManager.instance.GetJson(
                    "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/Event.json",
                    (s) => { Debug.Log($"{s}"); }));
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                var agentAddress = "50B8AA5Fd180Cf658Eb1d32926473715c1188476";
                var prefix = "DCC CONNECT";
                var message = "1675664730900";
                var signData = Encoding.UTF8.GetBytes($"{prefix}{message}");

                var hexSignData = ByteUtil.Hex(signData);
                var hash = Helper.Util.ComputeHash(hexSignData);

                var privateKey =
                    new PrivateKey(
                        "5435417bf8372f9f08f83f661fd66f8ec25123ba530dcc80de6b5a1b326906dd");
                var singed = privateKey.Sign(ByteUtil.ParseHex(hash));
                var signature = ByteUtil.Hex(singed);

                var secp256K1Signature = new byte[64];
                var ss = new Secp256k1();
                ss.SignatureParseDer(secp256K1Signature, singed);

                var tt = ByteUtil.Hex(secp256K1Signature);
                // // ---------------
                // // var hashed = HashDigest<SHA256>.DeriveFrom(ByteUtil.ParseHex(hash));
                // // var test1 = CryptoConfig.CryptoBackend.Sign(hashed, privateKey);
                // // var signature = ByteUtil.Hex(test1);
                // // --------------
                //
                // // var agentAddress = Game.instance.Agent.Address.ToHex();
                // // var prefix = "DCC CONNECT";
                // // var message = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                // // var signData = Encoding.UTF8.GetBytes($"{prefix}{message}");
                // //
                // // var hexSignData = ByteUtil.Hex(signData);
                // // var hash = Helper.Util.ComputeHash(hexSignData);
                // // var privateKey = Game.instance.Agent.PrivateKey;
                // //
                // // var singed = privateKey.Sign(ByteUtil.ParseHex(hash));
                // // var signature = ByteUtil.Hex(singed);
                //
                //
                // Debug.Log($"[hash] : {hash}");
                // Debug.Log($"[agentAddress] : {agentAddress}");
                // Debug.Log($"[data] : {signature}");
                // Debug.Log($"[message] : {message}");
                // var url =
                //     $"https://dcc-frontend-git-feat-connect-kimwz.vercel.app/connect?agentAddress=0x{agentAddress}&agentSignTimestamp={message}&agentSignature=0x{signature}";
                // Application.OpenURL(url);

                // EthSignWithLibplanet();
            }
        }

        public void EthSignWithLibplanet()
        {
            byte[] KeccakDigest(byte[] message)
            {
                var k = new KeccakDigest(256);
                var messageHash = new byte[k.GetDigestSize()];
                k.BlockUpdate(message, 0, message.Length);
                k.DoFinal(messageHash, 0);
                return messageHash;
            }

            var addr = ByteUtil.ParseHex("3910052cBD9ce64F41069be0aC255dF022aB51f0");
            var sig = ByteUtil.ParseHex(
                "25c50c34150228df38ba9e173045f49f5c23c4314ddafd96964a2d6b3260988919df5eca17008545a5b68daf3631c553ef1a040fafa02d7892acdceef58f01351b");

            var prefix = "DCC CONNECT";
            var nonce = "1675664730900";
            var messageHash = KeccakDigest(Encoding.UTF8.GetBytes(prefix + nonce));

            // if we use personal_sign instead of eth_sign, we need a prefixed data.
            // see also:
            // * https://eips.ethereum.org/EIPS/eip-191
            // * https://github.com/ethereum/go-ethereum/pull/2940
            // * https://docs.metamask.io/guide/signing-data.html
            /*
            byte[] HashEtherPrefixedMessage(byte[] message)
            {
                var byteList = new List<byte>();
                var bytePrefix = new byte[] { 0x19 };
                var textBytePrefix = Encoding.UTF8.GetBytes("Ethereum Signed Message:\n" + message.Length);
                byteList.AddRange(bytePrefix);
                byteList.AddRange(textBytePrefix);
                byteList.AddRange(message);
                return KeccakDigest(byteList.ToArray());
            }
            messageHash = HashEtherPrefixedMessage(messageHash);
            */

            // if len(sig) == 64, we should mask s for parity.
            // also, we can recover address instead of require additional public key.
            // see also: https://eips.ethereum.org/EIPS/eip-2098
            var r = new BigInteger(sig[..32]);
            var s = new BigInteger(sig[32..64]);

            // we don't need private key itself after deriving public key.
            var privateKey =
                new PrivateKey(
                    "5435417bf8372f9f08f83f661fd66f8ec25123ba530dcc80de6b5a1b326906dd");
            var publicKey = privateKey.PublicKey;

            // ps and secp256k1Param are constant.
            var ps = SecNamedCurves.GetByName("secp256k1");
            var secp256k1Param = new ECDomainParameters(ps.Curve, ps.G, ps.N, ps.H);
            var pubKey = new ECPublicKeyParameters("ECDSA",
                secp256k1Param.Curve.DecodePoint(publicKey.Format(false)), secp256k1Param);

            // it wouldn't be reached when if the remote side also uses secp256k1 (because it doesn't allow high S).
            var otherS = secp256k1Param.N.Subtract(s);
            if (s.CompareTo(otherS) == 1)
            {
                s = otherS;
            }

            var verifier = new ECDsaSigner();
            verifier.Init(false, pubKey);

            var t = verifier.VerifySignature(messageHash, r, s);
            Debug.Log($"{t}");
            // Assert.True();
        }


        [Serializable]
        public class SignatureResult
        {
            public string agentAddress;
            public string agentSignTimestamp;
            public string agentSignature;
        }
#endif
    }
}
