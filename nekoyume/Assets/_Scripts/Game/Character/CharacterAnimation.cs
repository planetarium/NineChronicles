using System;
using System.Collections.Generic;

namespace Nekoyume.Game.Character
{
    public static class CharacterAnimation
    {
        public enum Type
        {
            // todo: `Appear`와 `Disappear`는 없어질 예정.
            Appear,
            Disappear,
            Standing,
            StandingToIdle,
            Idle,
            Touch,
            Run,
            Attack,
            Casting,
            CastingAttack,
            CriticalAttack,
            Hit,
            Die,
            Win,
            Greeting,
            Emotion,
        }

        public static readonly List<Type> List = new List<Type>();

        static CharacterAnimation()
        {
            var values = Enum.GetValues(typeof(Type));
            foreach (var value in values)
            {
                List.Add((Type) value);
            }
        }
    }

    public class InvalidCharacterAnimationTypeException : Exception
    {
        public InvalidCharacterAnimationTypeException(string message) : base(message)
        {
        }
    }
}
