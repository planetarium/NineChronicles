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

        // ToDo. Lower로 끝나는 변수들은 애니메이션 리소스 이름이 변경된 후 제거될 예정입니다.
        public static readonly string AppearLower = nameof(Type.Appear).ToLower();
        public static readonly string IdleLower = nameof(Type.Idle).ToLower();
        public static readonly string RunLower = nameof(Type.Run).ToLower();
        public static readonly string AttackLower = nameof(Type.Attack).ToLower();
        public static readonly string CastingLower = nameof(Type.Casting).ToLower();
        public static readonly string HitLower = nameof(Type.Hit).ToLower();
        public static readonly string DieLower = nameof(Type.Die).ToLower();
        public static readonly string DisappearLower = nameof(Type.Disappear).ToLower();
        
        public static readonly List<Type> List = new List<Type>();
        
        // ToDo. 애니메이션 리소스 이름이 변경된 후 제거될 예정입니다.
        public static readonly Dictionary<Type, string> Lowers = new Dictionary<Type, string>();

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
            
            Lowers.Add(Type.Appear, AppearLower);
            Lowers.Add(Type.Idle, IdleLower);
            Lowers.Add(Type.Run, RunLower);
            Lowers.Add(Type.Attack, AttackLower);
            Lowers.Add(Type.Casting, CastingLower);
            Lowers.Add(Type.Hit, HitLower);
            Lowers.Add(Type.Die, DieLower);
            Lowers.Add(Type.Disappear, DisappearLower);
        } 
    }
}
