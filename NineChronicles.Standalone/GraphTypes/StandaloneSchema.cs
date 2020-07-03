using System;
using GraphQL.Types;
using GraphQL.Utilities;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneSchema : Schema
    {
        public StandaloneSchema()
        {
        }

        public StandaloneSchema(IServiceProvider serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<StandaloneQuery>();
            Mutation = serviceProvider.GetRequiredService<StandaloneMutation>();
            Subscription = serviceProvider.GetRequiredService<StandaloneSubscription>();
            Services = serviceProvider;
        }
    }
}
