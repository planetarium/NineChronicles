using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NineChronicles.Standalone.GraphTypes;
using NineChronicles.Standalone.Properties;


namespace NineChronicles.Standalone.Controllers
{
    [ApiController]
    public class GraphQLController : ControllerBase
    {
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

                var nineChroniclesNodeService = StandaloneServices.CreateHeadless(nineChroniclesProperties);
                StandaloneContext.NineChroniclesNodeService = nineChroniclesNodeService;
                StandaloneContext.BlockChain = nineChroniclesNodeService.Swarm.BlockChain;
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
    }
}
