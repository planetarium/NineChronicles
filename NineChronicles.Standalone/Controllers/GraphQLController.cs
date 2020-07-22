using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.KeyStore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model.State;
using NineChronicles.Standalone.GraphTypes;
using NineChronicles.Standalone.Properties;
using Serilog;


namespace NineChronicles.Standalone.Controllers
{
    [ApiController]
    public class GraphQLController : ControllerBase
    {
        private ConcurrentDictionary<Address, long> NotificationRecords { get; }
            = new ConcurrentDictionary<Address, long>();
        private StandaloneContext StandaloneContext { get; }

        public const string InitializeStandaloneEndpoint = "/initialize-standalone";

        public const string RunStandaloneEndpoint = "/run-standalone";

        public GraphQLController(StandaloneContext standaloneContext)
        {
            StandaloneContext = standaloneContext;
        }

        [HttpPost(InitializeStandaloneEndpoint)]
        public IActionResult InitializeStandAlone(
            [FromBody] ServiceBindingProperties properties
        )
        {
            if (properties.AppProtocolVersion is null)
            {
                BadRequest($"{nameof(properties.AppProtocolVersion)} must be present.");
            }

            if (properties.GenesisBlockPath is null)
            {
                BadRequest($"{properties.GenesisBlockPath} must be present.");
            }

            try
            {
                var nodeServiceProperties = NineChroniclesNodeServiceProperties
                    .GenerateLibplanetNodeServiceProperties(
                        properties.AppProtocolVersion,
                        properties.GenesisBlockPath,
                        properties.SwarmHost,
                        properties.SwarmPort,
                        properties.MinimumDifficulty,
                        properties.PrivateKeyString,
                        properties.StoreType,
                        properties.StorePath,
                        100,
                        properties.IceServerStrings,
                        properties.PeerStrings,
                        properties.NoTrustedStateValidators,
                        properties.TrustedAppProtocolVersionSigners,
                        properties.NoMiner,
                        true);

                var rpcServiceProperties = NineChroniclesNodeServiceProperties
                    .GenerateRpcNodeServiceProperties(
                        properties.RpcListenHost,
                        properties.RpcListenPort);

                var nineChroniclesProperties = new NineChroniclesNodeServiceProperties
                {
                    Rpc = rpcServiceProperties,
                    Libplanet = nodeServiceProperties
                };

                NineChroniclesNodeService nineChroniclesNodeService = StandaloneServices.CreateHeadless(
                    nineChroniclesProperties,
                    StandaloneContext,
                    ignoreBootstrapFailure: false
                );
                StandaloneContext.NineChroniclesNodeService = nineChroniclesNodeService;
                StandaloneContext.BlockChain = nineChroniclesNodeService.Swarm.BlockChain;
                StandaloneContext.BlockChain.TipChanged += NotifyRefillActionPoint;
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.ToString());
            }
            return Ok("Node service was initialized.");
        }

        [HttpPost(RunStandaloneEndpoint)]
        public IActionResult RunStandAlone()
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();

            if (StandaloneContext.NineChroniclesNodeService is null)
            {
                var errorMessage =
                    $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.NineChroniclesNodeService)} is null. " +
                    $"You should request {InitializeStandaloneEndpoint} before this action.";
                return StatusCode(StatusCodes.Status412PreconditionFailed, errorMessage);
            }

            StandaloneContext.NineChroniclesNodeService.Run(hostBuilder, StandaloneContext.CancellationToken);
            return Ok("Node service started.");
        }

        private void NotifyRefillActionPoint(
            object sender, BlockChain<PolymorphicAction<ActionBase>>.TipChangedEventArgs args)
        {
            List<Tuple<Guid, ProtectedPrivateKey>> tuples =
                StandaloneContext.KeyStore.List().ToList();
            if (!tuples.Any())
            {
                return;
            }

            IEnumerable<Address> playerAddresses = tuples.Select(tuple => tuple.Item2.Address);
            var chain = StandaloneContext.BlockChain;
            List<IValue> states = playerAddresses
                .Select(addr => chain.GetState(addr))
                .Where(value => !(value is null))
                .ToList();

            if (!states.Any())
            {
                return;
            }

            var agentStates =
                states.Select(state => new AgentState((Bencodex.Types.Dictionary) state));
            var avatarStates = agentStates.SelectMany(agentState =>
                agentState.avatarAddresses.Values.Select(address =>
                    new AvatarState((Bencodex.Types.Dictionary) chain.GetState(address))));

            bool IsDailyRewardRefilled(long dailyRewardReceivedIndex)
            {
                return args.Index >= dailyRewardReceivedIndex + GameConfig.DailyRewardInterval;
            }

            bool NeedsRefillNotification(AvatarState avatarState)
            {
                if (NotificationRecords.TryGetValue(avatarState.address, out long record))
                {
                    return avatarState.dailyRewardReceivedIndex != record
                           && IsDailyRewardRefilled(avatarState.dailyRewardReceivedIndex);
                }

                return IsDailyRewardRefilled(avatarState.dailyRewardReceivedIndex);
            }

            var avatarStatesToSendNotification = avatarStates
                .Where(NeedsRefillNotification)
                .ToList();

            if (avatarStatesToSendNotification.Any())
            {
                var notification = new Notification(NotificationEnum.Refill);
                StandaloneContext.NotificationSubject.OnNext(notification);
            }

            foreach (var avatarState in avatarStatesToSendNotification)
            {
                Log.Debug(
                    "Record notification for {AvatarAddress}",
                    avatarState.address.ToHex());
                NotificationRecords[avatarState.address] = avatarState.dailyRewardReceivedIndex;
            }
        }
    }
}
