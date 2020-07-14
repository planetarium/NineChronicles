using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public sealed class NotificationType : ObjectGraphType<Notification>
    {
        public NotificationType()
        {
            Field<NonNullGraphType<NotificationEnumType>>(
                name: "type",
                description: "The type of Notification.",
                resolve: context => context.Source.Type);
        }
    }
}
