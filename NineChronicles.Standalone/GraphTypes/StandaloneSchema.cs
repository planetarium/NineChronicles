using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneSchema : Schema
    {
        public StandaloneSchema(StandaloneContext standaloneContext)
        {
            Query = new StandaloneQuery(standaloneContext);
            Mutation = new StandaloneMutation(standaloneContext);
            Subscription = new StandaloneSubscription(standaloneContext);
        }
    }
}
