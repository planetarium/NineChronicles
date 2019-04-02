using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class Dead : EventBase
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return null;
        }
    }
}
