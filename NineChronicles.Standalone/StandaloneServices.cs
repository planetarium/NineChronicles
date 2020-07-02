using System;
using System.Threading;
using System.Threading.Tasks;
using Libplanet.Net;
using Microsoft.Extensions.Hosting;
using NineChronicles.Standalone.GraphTypes;
using NineChronicles.Standalone.Properties;

namespace NineChronicles.Standalone
{
    public static class StandaloneServices
    {
        public static Task RunHeadlessAsync(
            NineChroniclesNodeServiceProperties properties,
            IHostBuilder hostBuilder,
            StandaloneContext standaloneContext = null,
            CancellationToken cancellationToken = default)
        {

            NineChroniclesNodeService service = CreateHeadless(properties, standaloneContext);
            return service.Run(hostBuilder, cancellationToken);
        }

        public static NineChroniclesNodeService CreateHeadless(
            NineChroniclesNodeServiceProperties properties,
            StandaloneContext standaloneContext = null
        )
        {
            Progress<PreloadState> progress = null;
            if (!(standaloneContext is null))
            {
                progress = new Progress<PreloadState>(state =>
                {
                    standaloneContext.PreloadStateSubject.OnNext(state);
                });
            }

            var service = new NineChroniclesNodeService(
                properties.Libplanet,
                properties.Rpc,
                preloadProgress: progress,
                ignoreBootstrapFailure: true);
            service.ConfigureStandaloneContext(standaloneContext);

            return service;
        }

        public static Task RunGraphQLAsync(
            GraphQLNodeServiceProperties graphQLProperties,
            IHostBuilder hostBuilder,
            CancellationToken cancellationToken = default)
        {
            var service = new GraphQLService(graphQLProperties);
            return service.Run(hostBuilder, cancellationToken);
        }

        internal static void ConfigureStandaloneContext(this NineChroniclesNodeService service, StandaloneContext standaloneContext)
        {
            if (!(standaloneContext is null))
            {
                standaloneContext.BlockChain = service.Swarm.BlockChain;
                service.BootstrapEnded.WaitAsync().ContinueWith((task) =>
                {
                    standaloneContext.BootstrapEnded = true;
                    standaloneContext.NodeStatusSubject.OnNext(standaloneContext.NodeStatus);
                });
                service.PreloadEnded.WaitAsync().ContinueWith((task) =>
                {
                    standaloneContext.PreloadEnded = true;
                    standaloneContext.NodeStatusSubject.OnNext(standaloneContext.NodeStatus);
                });
            }
        }
    }
}
