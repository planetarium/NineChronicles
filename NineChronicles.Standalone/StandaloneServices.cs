using System.Threading;
using System.Threading.Tasks;
using Libplanet.Standalone.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NineChronicles.Standalone.Properties;

namespace NineChronicles.Standalone.Executable
{
    public static class StandaloneServices
    {
        public static Task RunHeadlessAsync(
            NineChroniclesNodeServiceProperties properties,
            CancellationToken cancellationToken)
        {
            var service = new NineChroniclesNodeService(
                properties.Libplanet,
                properties.Rpc,
                ignoreBootstrapFailure: true);
            return service.Run(cancellationToken);
        }

        public static Task RunGraphQLAsync(
            GraphQLNodeServiceProperties graphQLProperties
        )
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();
            hostBuilder.ConfigureWebHostDefaults(builder =>
            {
                builder.UseStartup<GraphQLStartup>();
                builder.UseUrls($"http://{graphQLProperties.GraphQLListenHost}:{graphQLProperties.GraphQLListenPort}/");
            });
            return hostBuilder.RunConsoleAsync();
        }
    }
}
