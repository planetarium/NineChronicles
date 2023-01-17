using Coffee.UIEffects;
using Nekoyume.Game.VFX;
using Nekoyume.Model.Buff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nekoyume.Helper
{
    public static class BuffHelper
    {
        private static BuffVFXScriptableObject _vfxData;

        private static BuffVFXScriptableObject VFXData
        {
            get
            {
                if (_vfxData == null)
                {
                    _vfxData = Resources.Load<BuffVFXScriptableObject>(
                        "ScriptableObject/BuffVFXData");
                }

                return _vfxData;
            }
        }

        public static GameObject GetCastingVFXPrefab(Buff buff)
        {
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == buff.BuffInfo.Id);
            if (overrideData == null)
            {
                if (buff is StatBuff statBuff)
                {
                    var modifier = statBuff.GetModifier();
                    var isPositive = modifier.Value >= 0;
                    var data = VFXData.DataList
                        .FirstOrDefault(x => x.StatType == modifier.StatType);
                    return data == null ? VFXData.FallbackCastingVFX :
                        isPositive ? data.PlusCastingVFX : data.MinusCastingVFX;
                }

                return VFXData.FallbackCastingVFX;
            }
            else
            {
                return overrideData.CastingVFX;
            }
        }

        public static GameObject GetBuffVFXPrefab(Buff buff)
        {
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == buff.BuffInfo.Id);
            if (overrideData == null)
            {
                if (buff is StatBuff statBuff)
                {
                    var modifier = statBuff.GetModifier();
                    var isPositive = modifier.Value >= 0;
                    var data = VFXData.DataList
                        .FirstOrDefault(x => x.StatType == modifier.StatType);
                    return data == null ? VFXData.FallbackBuffVFX :
                        isPositive ? data.PlusVFX : data.MinusVFX;
                }

                return VFXData.FallbackBuffVFX;
            }
            else
            {
                return overrideData.BuffVFX;
            }
        }

        public static Sprite GetBuffIcon(Buff buff)
        {
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == buff.BuffInfo.Id);
            if (overrideData == null)
            {
                if (buff is StatBuff statBuff)
                {
                    var modifier = statBuff.GetModifier();
                    var isPositive = modifier.Value >= 0;
                    var data = VFXData.DataList
                        .FirstOrDefault(x => x.StatType == modifier.StatType);
                    return data == null ? VFXData.FallbackIcon :
                        isPositive ? data.PlusIcon : data.MinusIcon;
                }

                return VFXData.FallbackIcon;
            }
            else
            {
                return overrideData.Icon;
            }
        }
    }
}
