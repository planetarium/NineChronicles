using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneMutation : ObjectGraphType
    {
        public StandaloneMutation()
        {
            Field<BooleanGraphType>("start",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>>
                    {Name = "name"}));
        }
    }
}
