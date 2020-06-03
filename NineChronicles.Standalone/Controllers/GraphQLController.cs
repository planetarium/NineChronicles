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
        [HttpGet("/health-check")]
        public IActionResult HealthCheck()
        {
            return Ok("Hello!");
        }

        [HttpPost("/run-standalone")]
        public IActionResult RunStandAlone(
            [FromBody] ServiceBindingProperties properties
        )
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();

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
                        properties.NoMiner);

                var rpcServiceProperties = NineChroniclesNodeServiceProperties
                    .GenerateRpcNodeServiceProperties(
                        properties.RpcListenHost,
                        properties.RpcListenPort);

                var nineChroniclesProperties = new NineChroniclesNodeServiceProperties
                {
                    Rpc = rpcServiceProperties,
                    Libplanet = nodeServiceProperties
                };

                StandaloneServices.RunHeadlessAsync(
                    nineChroniclesProperties,
                    hostBuilder,
                    NodeCancellationContext.CancellationToken);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.ToString());
            }
            return Ok("Node services start.");
        }
    }
}
