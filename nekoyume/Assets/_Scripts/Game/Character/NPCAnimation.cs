using System;
using System.Collections.Generic;

namespace Nekoyume.Game.Character
{
    public static class NPCAnimation
    {
        public enum Type
        {
            Appear_01,
            Appear_02,
            Appear_03,
            Greeting_01,
            Greeting_02,
            Greeting_03,
            Open_01,
            Open_02,
            Open_03,
            Idle_01,
            Idle_02,
            Idle_03,
            Emotion_01,
            Emotion_02,
            Emotion_03,
            Emotion_04,
            Emotion_05,
            Touch_01,
            Touch_02,
            Touch_03,
            Loop_01,
            Loop_02,
            Loop_03,
            Disappear_01,
            Disappear_02,
            Disappear_03,
            Appear,
            Over,
            Click,
        }
        
        public static readonly List<Type> List = new List<Type>();

        static NPCAnimation()
        {
            var values = Enum.GetValues(typeof(Type));
            foreach (var value in values)
            {
                List.Add((Type) value);
            }
        } 
    }
    
    public class InvalidNPCAnimationTypeException : Exception
    {
        public InvalidNPCAnimationTypeException(string message) : base(message)
        {
        }
    }
}
