namespace NineChronicles.Standalone.GraphTypes
{
    public class Notification
    {
        public NotificationEnum Type { get; set; }

        public Notification(NotificationEnum type)
        {
            Type = type;
        }
    }
}
