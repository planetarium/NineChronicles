using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using NineChronicles.Standalone.Exceptions;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone.Properties
{
    public class NineChroniclesNodeServiceProperties
    {
        public RpcNodeServiceProperties Rpc { get; set; }

        public LibplanetNodeServiceProperties<NineChroniclesActionType> Libplanet { get; set; }

        public static LibplanetNodeServiceProperties<NineChroniclesActionType>
            GenerateLibplanetNodeServiceProperties(
                string appProtocolVersionToken = null,
                string genesisBlockPath = null,
                string swarmHost = null,
                ushort? swarmPort = null,
                int minimumDifficulty = 5000000,
                string privateKeyString = null,
                string storeType = null,
                string storePath = null,
                string[] iceServerStrings = null,
                string[] peerStrings = null,
                bool noTrustedStateValidators = false,
                string[] trustedAppProtocolVersionSigners = null,
                bool noMiner = false,
                bool render = false)
        {
            var privateKey = string.IsNullOrEmpty(privateKeyString)
                ? new PrivateKey()
                : new PrivateKey(ByteUtil.ParseHex(privateKeyString));

            peerStrings ??= Array.Empty<string>();
            iceServerStrings ??= Array.Empty<string>();

            var iceServers = iceServerStrings.Select(LoadIceServer).ToImmutableArray();
            var peers = peerStrings.Select(LoadPeer).ToImmutableArray();

            IImmutableSet<Address> trustedStateValidators;
            if (noTrustedStateValidators)
            {
                trustedStateValidators = ImmutableHashSet<Address>.Empty;
            }
            else
            {
                trustedStateValidators = peers.Select(p => p.Address).ToImmutableHashSet();
            }

            return new LibplanetNodeServiceProperties<NineChroniclesActionType>
            {
                Host = swarmHost,
                Port = swarmPort,
                AppProtocolVersion = AppProtocolVersion.FromToken(appProtocolVersionToken),
                TrustedAppProtocolVersionSigners = trustedAppProtocolVersionSigners
                    ?.Select(s => new PublicKey(ByteUtil.ParseHex(s)))
                    ?.ToHashSet(),
                GenesisBlockPath = genesisBlockPath,
                NoMiner = noMiner,
                PrivateKey = privateKey,
                IceServers = iceServers,
                Peers = peers,
                TrustedStateValidators = trustedStateValidators,
                StoreType = storeType,
                StorePath = storePath,
                StoreStatesCacheSize = 5000,
                MinimumDifficulty = minimumDifficulty
            };
        }

        public static RpcNodeServiceProperties GenerateRpcNodeServiceProperties(
            bool rpcServer = false,
            string rpcListenHost = "0.0.0.0",
            int? rpcListenPort = null)
        {

            if (string.IsNullOrEmpty(rpcListenHost))
            {
                throw new ArgumentNullException(
                    "--rpc-listen-host is required when --rpc-server is present.");
            }

            if (!(rpcListenPort is int rpcPortValue))
            {
                throw new ArgumentNullException(
                    "--rpc-listen-port is required when --rpc-server is present.");
            }

            return new RpcNodeServiceProperties
            {
                RpcServer = rpcServer,
                RpcListenHost = rpcListenHost,
                RpcListenPort = rpcPortValue
            };
        }

        private static IceServer LoadIceServer(string iceServerInfo)
        {
            try
            {
                var uri = new Uri(iceServerInfo);
                string[] userInfo = uri.UserInfo.Split(':');

                return new IceServer(new[] {uri}, userInfo[0], userInfo[1]);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    throw e.InnerException;

                throw new IceServerInvalidException(
                    $"--ice-server '{iceServerInfo}' seems invalid.\n" +
                    $"{e.GetType()} {e.Message}\n" +
                    $"{e.StackTrace}");
            }
        }

        private static BoundPeer LoadPeer(string peerInfo)
        {
            var tokens = peerInfo.Split(',');
            if (tokens.Length != 3)
            {
                throw new PeerInvalidException(
                    $"--peer '{peerInfo}', should have format <pubkey>,<host>,<port>");
            }

            if (!(tokens[0].Length == 130 || tokens[0].Length == 66))
            {
                throw new PeerInvalidException(
                    $"--peer '{peerInfo}', a length of public key must be 130 or 66 in hexadecimal," +
                    $" but the length of given public key '{tokens[0]}' doesn't.");
            }

            try
            {
                var pubKey = new PublicKey(ByteUtil.ParseHex(tokens[0]));
                var host = tokens[1];
                var port = int.Parse(tokens[2]);

                // FIXME: It might be better to make Peer.AppProtocolVersion property nullable...
                return new BoundPeer(
                    pubKey,
                    new DnsEndPoint(host, port),
                    default(AppProtocolVersion));
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    throw e.InnerException;

                throw new PeerInvalidException(
                    $"--peer '{peerInfo}' seems invalid.\n" +
                    $"{e.GetType()} {e.Message}\n" +
                    $"{e.StackTrace}");
            }
        }
    }
}
