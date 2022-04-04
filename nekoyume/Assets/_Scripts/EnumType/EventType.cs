namespace Nekoyume.EnumType
{
    public enum EventType
    {
        Default = 0,
        Christmas = 1,
        Valentine = 2,
        // Revomon Collaboration
        // Extend ear and tail costume when create a avatar.
        // UTC 2022-04-04 15:00:00 ~ 2022-04-19 14:59:59
        // Do not repeat every year.
        // Remove at the end of the event.
        Revomon = 3,
        // This will removed with Revomon.
        BeforeRevomon = 100,
        // This will removed with Revomon.
        AfterRevomon = 101,
    }
}
