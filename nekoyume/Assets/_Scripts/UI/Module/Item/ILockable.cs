namespace Nekoyume.UI.Module
{
    public interface ILockable
    {
        bool IsLocked { get; }
        void Lock();
        void Unlock();
    }
}
