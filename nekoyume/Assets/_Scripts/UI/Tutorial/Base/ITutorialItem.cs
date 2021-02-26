namespace Nekoyume.UI
{
    public interface ITutorialItem
    {
        void Play<T>(T data, System.Action callback) where T : ITutorialData;
        void Stop(System.Action callback);
    }
}
