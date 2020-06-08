using System.Threading;
using System.Threading.Tasks;
using GraphQL.Server;
using Libplanet.KeyStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NineChronicles.Standalone.GraphTypes;
using NineChronicles.Standalone.Properties;

namespace NineChronicles.Standalone
{
    public class GraphQLService
    {
        private GraphQLNodeServiceProperties GraphQlNodeServiceProperties { get; }

        public GraphQLService(GraphQLNodeServiceProperties properties)
        {
            GraphQlNodeServiceProperties = properties;
        }

        public async Task Run(
            IHostBuilder hostBuilder,
            CancellationToken cancellationToken = default)
        {
            var listenHost = GraphQlNodeServiceProperties.GraphQLListenHost;
            var listenPort = GraphQlNodeServiceProperties.GraphQLListenPort;

            await hostBuilder.ConfigureWebHostDefaults(builder =>
            {
                builder.UseStartup<GraphQLStartup>();
                builder.UseUrls($"http://{listenHost}:{listenPort}/");
            }).RunConsoleAsync(cancellationToken);
        }

        class GraphQLStartup
        {
            public GraphQLStartup(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            public IConfiguration Configuration { get; }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddCors(options =>
                    options.AddPolicy(
                        "AllowAllOrigins",
                        builder =>
                            builder.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader()
                    )
                );

                services.AddControllers();

                var standaloneContext = new StandaloneContext
                {
                    KeyStore = Web3KeyStore.DefaultKeyStore,
                };

                services
                    .AddSingleton<StandaloneSchema>()
                    .AddSingleton<StandaloneContext>(standaloneContext)
                    .AddGraphQL((provider, options) =>
                    {
                        options.EnableMetrics = true;
                        options.ExposeExceptions = true;
                    })
                    .AddSystemTextJson()
                    .AddWebSockets()
                    .AddDataLoader()
                    .AddGraphTypes(typeof(StandaloneSchema));
            }

            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                app.UseCors("AllowAllOrigins");

                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

                // WebSocket으로 운영합니다.
                app.UseWebSockets();
                app.UseGraphQLWebSockets<StandaloneSchema>("/graphql");
                app.UseGraphQL<StandaloneSchema>("/graphql");

                // /ui/playground 옵션을 통해서 Playground를 사용할 수 있습니다.
                app.UseGraphQLPlayground();
            }
        }

    }
}
