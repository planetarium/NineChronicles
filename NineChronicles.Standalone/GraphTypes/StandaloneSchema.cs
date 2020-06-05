using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneSchema : Schema
    {
        public StandaloneSchema(StandaloneContext standaloneContext)
        {
            Query = new StandaloneQuery(standaloneContext);
            Mutation = new StandaloneMutation();
            Subscription = new StandaloneSubscription(standaloneContext);
        }
    }
}
