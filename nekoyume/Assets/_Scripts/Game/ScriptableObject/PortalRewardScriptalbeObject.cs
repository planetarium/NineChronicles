using Nekoyume.UI;
using Nekoyume.UI.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "Portal_Reward", menuName = "Scriptable Object/PortalReward",
        order = int.MaxValue)]
    public class PortalRewardScriptalbeObject : ScriptableObject
    {
        public List<int> levelData;
        public List<int> stageData;
    }
}
