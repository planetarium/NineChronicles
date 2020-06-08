using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public class Mutation : ObjectGraphType
    {
        public Mutation()
        {
            Field<BooleanGraphType>("start",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>>
                    {Name = "name"}));
        }
    }
}
