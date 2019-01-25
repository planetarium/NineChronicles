using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Trigger;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Model
{
    [Serializable]
    public class Spawn : EventBase
    {
        public override void Execute(Stage stage)
        {
            if (character is Player)
            {
                stage.SpawnPlayer();
            }
            else if (character is Monster)
            {
                stage.SpawnMonster((Monster)character);
            }
        }
    }
}
