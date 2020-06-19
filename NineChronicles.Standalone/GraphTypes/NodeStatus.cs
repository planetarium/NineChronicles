using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public class NodeStatusType : ObjectGraphType<NodeStatusType>
    {
        public bool BootstrapEnded { get; set; }

        public bool PreloadEnded { get; set; }

        public NodeStatusType()
        {
            Field<NonNullGraphType<BooleanGraphType>>(name: "bootstrapEnded",
                resolve: context => context.Source.BootstrapEnded);
            Field<NonNullGraphType<BooleanGraphType>>(name: "preloadEnded",
                resolve: context => context.Source.PreloadEnded);
        }
    }
}
