using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.Game.Character
{
    public static class PetAnimation
    {
        public enum Type
        {
            Idle,
            Special,
            Interaction,
            BattleStart,
            BattleEnd
        }

        public static readonly List<Type> List = new();

        static PetAnimation()
        {
            var values = Enum.GetValues(typeof(Type));
            List.AddRange(values.Cast<Type>());
        }
    }

    public class InvalidPetAnimationTypeException : Exception
    {
        public InvalidPetAnimationTypeException(string message) : base(message)
        {
        }
    }
}
