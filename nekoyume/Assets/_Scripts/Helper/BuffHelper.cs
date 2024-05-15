using Nekoyume.Model.Buff;
using Nekoyume.Model.Stat;
using System.Linq;
using UnityEngine;

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
                    var modifier   = statBuff.GetModifier();
                    var isPositive = modifier.Value >= 0;
                    var data = VFXData.DataList
                        .FirstOrDefault(x => x.StatType == modifier.StatType);
                    return data == null ? VFXData.FallbackCastingVFX :
                        isPositive      ? data.PlusCastingVFX : data.MinusCastingVFX;
                }

                return VFXData.FallbackCastingVFX;
            }
            return overrideData.CastingVFX;
        }

        public static GameObject GetCastingVFXPrefab(int buffId)
        {
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == buffId);

            return overrideData == null ? VFXData.FallbackCastingVFX : overrideData.CastingVFX;
        }

        public static GameObject GetBuffVFXPrefab(Buff buff)
        {
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == buff.BuffInfo.Id);
            if (overrideData == null)
            {
                if (buff is StatBuff statBuff)
                {
                    var modifier   = statBuff.GetModifier();
                    var isPositive = modifier.Value >= 0;
                    var data = VFXData.DataList
                        .FirstOrDefault(x => x.StatType == modifier.StatType);
                    return data == null ? VFXData.FallbackBuffVFX :
                        isPositive      ? data.PlusVFX : data.MinusVFX;
                }

                return VFXData.FallbackBuffVFX;
            }
            return overrideData.BuffVFX;
        }

        public static GameObject GetBuffVFXPrefab(int buffId)
        {
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == buffId);
            return overrideData == null ? VFXData.FallbackBuffVFX : overrideData.BuffVFX;
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

        public static Sprite GetBuffOverrideIcon(int id)
        {
            var overrideData = VFXData.OverrideDataList
                    .FirstOrDefault(x => x.Id == id);
            if(overrideData == null)
            {
                return VFXData.FallbackIcon;
            }
            return overrideData.Icon;
        }

        public static Sprite GetStatBuffIcon(StatType statType, bool isDebuff)
        {
            var data = VFXData.DataList.FirstOrDefault(x => x.StatType == statType);
            return data == null ? VFXData.FallbackIcon :
                isDebuff ? data.MinusIcon : data.PlusIcon;
        }

        public static Vector3 GetDefaultBuffPosition()
        {
            return VFXData.FallbackPosition;
        }

        public static Vector3 GetBuffPosition(int id, bool isCasting = false)
        {
            var overrideData = VFXData.BuffPosOverrideDataList
                .FirstOrDefault(x => x.Id == id && x.IsCasting == isCasting);
            return overrideData?.Position ?? VFXData.FallbackPosition;
        }
    }
}
