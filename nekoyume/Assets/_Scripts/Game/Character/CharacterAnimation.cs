using System.Collections.Generic;

namespace Nekoyume.Game.Character
{
    public class CharacterAnimation
    {
        public enum Type
        {
            Appear,
            Idle,
            Run,
            Attack,
            Casting,
            Hit,
            Die,
            Disappear
        }
        
        public static readonly List<Type> List = new List<Type>();

        static CharacterAnimation()
        {
            List.Add(Type.Appear);
            List.Add(Type.Idle);
            List.Add(Type.Run);
            List.Add(Type.Attack);
            List.Add(Type.Casting);
            List.Add(Type.Hit);
            List.Add(Type.Die);
            List.Add(Type.Disappear);
        } 
    }
}
