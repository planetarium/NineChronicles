namespace Nekoyume.Model.Garages
{
    public interface IGarage<in T1, in T2> where T1 : IGarage<T1, T2>
    {
        void Load(T2 amount);
        void Deliver(T1 to, T2 amount);
        void Unload(T2 amount);
    }
}
