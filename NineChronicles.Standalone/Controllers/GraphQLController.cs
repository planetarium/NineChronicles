using System;
using System.Collections.Concurrent;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
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
                    StandaloneContext
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
            var privateKey = StandaloneContext.NineChroniclesNodeService.PrivateKey;
            var chain = StandaloneContext.BlockChain;
            IValue state = chain.GetState(privateKey.ToAddress());

            if (state is null)
            {
                return;
            }

            var agentState = new AgentState((Bencodex.Types.Dictionary) state);
            var avatarStates = agentState.avatarAddresses.Values.Select(address =>
                new AvatarState((Bencodex.Types.Dictionary) chain.GetState(address)));
            var avatarStatesCanRefill =
                avatarStates.Where(avatarState =>
                        NotificationRecords.TryGetValue(avatarState.address, out long notificationRecord)
                            ? avatarState.dailyRewardReceivedIndex != notificationRecord
                            : args.Index >= avatarState.dailyRewardReceivedIndex + GameConfig.DailyRewardInterval)
                .ToList();

            if (avatarStatesCanRefill.Any())
            {
                var notification = new Notification(NotificationEnum.Refill);
                StandaloneContext.NotificationSubject.OnNext(notification);
            }

            foreach (var avatarState in avatarStatesCanRefill)
            {
                Log.Debug("Record notification for {AvatarAddress}", avatarState.address.ToHex());
                NotificationRecords[avatarState.address] = avatarState.dailyRewardReceivedIndex;
            }
        }
    }
}
