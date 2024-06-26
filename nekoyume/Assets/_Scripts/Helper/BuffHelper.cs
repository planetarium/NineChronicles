using Nekoyume.Model.Buff;
using Nekoyume.Model.Stat;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.Game;
using Nekoyume.TableData;
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
                    _vfxData = Resources.Load<BuffVFXScriptableObject>("ScriptableObject/BuffVFXData");
                }

                return _vfxData;
            }
        }

        public static GameObject GetCastingVFXPrefab(Buff buff)
        {
            var id               = buff.BuffInfo.Id;
            var actionBuffCastingPrefab = GetActionBuffCastingPrefab(id);
            if (actionBuffCastingPrefab != null)
            {
                return actionBuffCastingPrefab;
            }
            
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == buff.BuffInfo.Id);

            if (overrideData != null)
            {
                return overrideData.CastingVFX;
            }

            if (buff is not StatBuff statBuff)
            {
                return VFXData.FallbackCastingVFX;
            }
            
            var modifier   = statBuff.GetModifier();
            var isPositive = modifier.Value >= 0;
            var data = VFXData.DataList
                .FirstOrDefault(x => x.StatType == modifier.StatType);
            return data == null ? VFXData.FallbackCastingVFX :
                isPositive      ? data.PlusCastingVFX : data.MinusCastingVFX;
        }

        public static GameObject GetCastingVFXPrefab(int buffId)
        {
            var actionBuffCastingPrefab = GetActionBuffCastingPrefab(buffId);
            if (actionBuffCastingPrefab != null)
            {
                return actionBuffCastingPrefab;
            }
            
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == buffId);

            return overrideData == null ? VFXData.FallbackCastingVFX : overrideData.CastingVFX;
        }
        
        [CanBeNull]
        private static GameObject GetActionBuffCastingPrefab(int id)
        {
            var actionSheet   = GetActionBuffSheet();
            var hasActionBuff = actionSheet.TryGetValue(id, out var actionBuffRow);
            if (!hasActionBuff)
            {
                return null;
            }
            
            var actionBuffType     = actionBuffRow.ActionBuffType;
            var actionBuffDataList = VFXData.ActionBuffVFXOverrideDataList;
            var actionOverrideData = actionBuffDataList
                .FirstOrDefault(x => x.Type == actionBuffType);
            return actionOverrideData?.CastingVFX;
        }

        public static GameObject GetBuffVFXPrefab(Buff buff)
        {
            var id = buff.BuffInfo.Id;
            var actionBuffPrefab = GetActionBuffPrefab(id);
            if (actionBuffPrefab != null)
            {
                return actionBuffPrefab;
            }
            
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == id);
            if (overrideData != null)
            {
                return overrideData.BuffVFX;
            }
            
            if (buff is not StatBuff statBuff)
            {
                return VFXData.FallbackBuffVFX;
            }
            var modifier   = statBuff.GetModifier();
            var isPositive = modifier.Value >= 0;
            var data = VFXData.DataList
                .FirstOrDefault(x => x.StatType == modifier.StatType);
            return data == null ? VFXData.FallbackBuffVFX :
                isPositive      ? data.PlusVFX : data.MinusVFX;
        }

        public static GameObject GetBuffVFXPrefab(int buffId)
        {
            var actionBuffPrefab = GetActionBuffPrefab(buffId);
            if (actionBuffPrefab != null)
            {
                return actionBuffPrefab;
            }
            
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == buffId);
            return overrideData == null ? VFXData.FallbackBuffVFX : overrideData.BuffVFX;
        }
        
        [CanBeNull]
        private static GameObject GetActionBuffPrefab(int id)
        {
            var actionSheet   = GetActionBuffSheet();
            var hasActionBuff = actionSheet.TryGetValue(id, out var actionBuffRow);
            if (!hasActionBuff)
            {
                return null;
            }
            
            var actionBuffType     = actionBuffRow.ActionBuffType;
            var actionBuffDataList = VFXData.ActionBuffVFXOverrideDataList;
            var actionOverrideData = actionBuffDataList
                .FirstOrDefault(x => x.Type == actionBuffType);
            return actionOverrideData?.BuffVFX;
        }

        public static Sprite GetBuffIcon(Buff buff)
        {
            var id = buff.BuffInfo.Id;
            var actionBuffIcon = GetActionBuffIcon(id);
            if (actionBuffIcon != null)
            {
                return actionBuffIcon;
            }
            
            var overrideData = VFXData.OverrideDataList
                .FirstOrDefault(x => x.Id == id);
            if (overrideData != null)
            {
                return overrideData.Icon;
            }
            
            if (buff is not StatBuff statBuff)
            {
                return VFXData.FallbackIcon;
            }
                
            var modifier   = statBuff.GetModifier();
            var isPositive = modifier.Value >= 0;
            var data = VFXData.DataList
                .FirstOrDefault(x => x.StatType == modifier.StatType);
            return data == null ? VFXData.FallbackIcon :
                isPositive      ? data.PlusIcon : data.MinusIcon;
        }

        public static Sprite GetBuffOverrideIcon(int id)
        {
            var actionBuffIcon = GetActionBuffIcon(id);
            if (actionBuffIcon != null)
            {
                return actionBuffIcon;
            }
            
            var overrideData = VFXData.OverrideDataList.FirstOrDefault(x => x.Id == id);
            return overrideData == null ? VFXData.FallbackIcon : overrideData.Icon;
        }

        [CanBeNull]
        private static Sprite GetActionBuffIcon(int id)
        {
            var actionSheet   = GetActionBuffSheet();
            var hasActionBuff = actionSheet.TryGetValue(id, out var actionBuffRow);
            if (!hasActionBuff)
            {
                return null;
            }
            
            var actionBuffType     = actionBuffRow.ActionBuffType;
            var actionBuffDataList = VFXData.ActionBuffVFXOverrideDataList;
            var actionOverrideData = actionBuffDataList
                .FirstOrDefault(x => x.Type == actionBuffType);
            return actionOverrideData?.Icon;
        }

        public static Sprite GetStatBuffIcon(StatType statType, bool isDebuff)
        {
            var data = VFXData.DataList.FirstOrDefault(x => x.StatType == statType);
            return data == null ? VFXData.FallbackIcon : isDebuff ? data.MinusIcon : data.PlusIcon;
        }

        public static Vector3 GetDefaultBuffPosition()
        {
            return VFXData.FallbackPosition;
        }

        public static Vector3 GetBuffPosition(int id, bool isCasting = false)
        {
            var actionSheet = GetActionBuffSheet();
            var hasActionBuff = actionSheet.TryGetValue(id, out var actionBuffRow);
            if (hasActionBuff)
            {
                var actionBuffType = actionBuffRow.ActionBuffType;
                var actionBuffDataList = VFXData.ActionBuffPosOverrideDataList;
                var actionOverrideData = actionBuffDataList
                    .FirstOrDefault(x => x.Type == actionBuffType && x.IsCasting == isCasting);
                if (actionOverrideData != null)
                {
                    return actionOverrideData.Position;
                }
            }
            
            var overrideData = VFXData.BuffPosOverrideDataList
                .FirstOrDefault(x => x.Id == id && x.IsCasting == isCasting);
            return overrideData?.Position ?? VFXData.FallbackPosition;
        }
        
        private static ActionBuffSheet GetActionBuffSheet()
        {
            return TableSheets.Instance.ActionBuffSheet;
        }
    }
}
