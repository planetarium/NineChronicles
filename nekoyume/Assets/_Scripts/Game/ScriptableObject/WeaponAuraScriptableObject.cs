using System.Collections.Generic;
using UnityEngine;
using Nekoyume.Game.ScriptableObject;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "VFX_WeaponAura", menuName = "Scriptable Object/Weapon Aura",
        order = int.MaxValue)]
    public class WeaponAuraScriptableObject : ScriptableObject
    {
        public List<GameObject> data;
    }
}
