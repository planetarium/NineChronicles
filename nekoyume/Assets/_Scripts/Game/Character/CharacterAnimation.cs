using System.Collections.Generic;

namespace Nekoyume.Game.Character
{
    public class CharacterAnimation
    {
        public const string Appear = "Appear";
        public const string Idle = "Idle";
        public const string Run = "Run";
        public const string Attack = "Attack";
        public const string Casting = "Casting";
        public const string Hit = "Hit";
        public const string Die = "Die";
        public const string Disappear = "Disappear";
        
        public static readonly string AppearLower = Appear.ToLower();
        public static readonly string IdleLower = Idle.ToLower();
        public static readonly string RunLower = Run.ToLower();
        public static readonly string AttackLower = Attack.ToLower();
        public static readonly string CastingLower = Casting.ToLower();
        public static readonly string HitLower = Hit.ToLower();
        public static readonly string DieLower = Die.ToLower();
        public static readonly string DisappearLower = Disappear.ToLower();
        
        public static readonly List<string> List = new List<string>();
        public static readonly Dictionary<string, string> Lowers = new Dictionary<string, string>();

        static CharacterAnimation()
        {
            List.Add(Appear);
            List.Add(Idle);
            List.Add(Run);
            List.Add(Attack);
            List.Add(Casting);
            List.Add(Hit);
            List.Add(Die);
            List.Add(Disappear);
            
            Lowers.Add(Appear, AppearLower);
            Lowers.Add(Idle, IdleLower);
            Lowers.Add(Run, RunLower);
            Lowers.Add(Attack, AttackLower);
            Lowers.Add(Casting, CastingLower);
            Lowers.Add(Hit, HitLower);
            Lowers.Add(Die, DieLower);
            Lowers.Add(Disappear, DisappearLower);
        } 
    }
}
