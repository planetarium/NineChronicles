using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneSchema : Schema
    {
        public StandaloneSchema(StandaloneContext standaloneContext)
        {
            Query = new Query();
            Mutation = new Mutation();
            Subscription = new StandaloneSubscription(standaloneContext);
        }
    }
}
