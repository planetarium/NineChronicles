using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneSchema : Schema
    {
        public StandaloneSchema(IStandaloneContext standaloneContext)
        {
            Query = new Query();
            Mutation = new Mutation();
            Subscription = new StandaloneSubscription(standaloneContext);
        }
    }
}
