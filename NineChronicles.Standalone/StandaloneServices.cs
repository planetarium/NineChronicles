using System.Threading;
using System.Threading.Tasks;
using Libplanet.Standalone.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NineChronicles.Standalone.Properties;

namespace NineChronicles.Standalone
{
    public static class StandaloneServices
    {
        public static Task RunHeadlessAsync(
            NineChroniclesNodeServiceProperties properties,
            IHostBuilder hostBuilder,
            CancellationToken cancellationToken = default)
        {
            var service = new NineChroniclesNodeService(
                properties.Libplanet,
                properties.Rpc,
                ignoreBootstrapFailure: true);
            return service.Run(hostBuilder, cancellationToken);
        }

        public static Task RunGraphQLAsync(
            GraphQLNodeServiceProperties graphQLProperties,
            IHostBuilder hostBuilder,
            CancellationToken cancellationToken = default)
        {
            var service = new GraphQLService(graphQLProperties);
            return service.Run(hostBuilder, cancellationToken);
        }
    }
}
