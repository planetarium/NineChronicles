using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine;
using Nekoyume;

namespace Nekoyume.Game.Util
{
    public class IngameDebugConsoleCommands
    {
        public static void Initailize()
        {
            DebugLogConsole.AddCommand("test", "desc", () =>
            {
                Debug.Log("TGestdd");
            });
        }
    }
}
