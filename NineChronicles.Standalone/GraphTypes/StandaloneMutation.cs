using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneMutation : ObjectGraphType
    {
        private StandaloneContext StandaloneContext { get; }

        public StandaloneMutation(StandaloneContext standaloneContext)
        {
            StandaloneContext = standaloneContext;

            Field<KeyStoreMutation>(
                name: "keyStore",
                resolve: context => standaloneContext.KeyStore);

            Field<ActivatedAccountsMutation>(
                name: "activatedAccounts",
                resolve: context => standaloneContext.NineChroniclesNodeService);
        }
    }
}
