using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public sealed class NotificationType : ObjectGraphType<Notification>
    {
        public NotificationType()
        {
            Field(n => n.Type);
        }
    }
}
