using TMPro;
using UnityEngine;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using System.Linq;
using UnityEngine.UI;
using Nekoyume.Helper;
using Nekoyume.Model.Stat;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Nekoyume.UI.Module.Common
{
    public class SkillPositionTooltip : PositionTooltip
    {
        private struct OptionDigest
        {
            public int SkillId;
            public int ChanceMin;
            public int ChanceMax;
            public long PowerMin;
            public long PowerMax;
            public int StatPowerRatioMin;
            public int StatPowerRatioMax;
            public StatType ReferencedStatType;
        }

        [SerializeField]
        protected TextMeshProUGUI cooldownText;

        [SerializeField]
        protected GameObject buffObject;

        [SerializeField]
        protected GameObject debuffObject;

        [SerializeField]
        protected Image buffIconImage;

        [SerializeField]
        protected TextMeshProUGUI buffStatTypeText;

        [SerializeField]
        protected Image debuffIconImage;

        [SerializeField]
        protected TextMeshProUGUI debuffStatTypeText;

        private const string VariableColorTag = "<color=#f5e3c0>";

        public void Show(SkillSheet.Row skillRow, EquipmentItemOptionSheet.Row optionRow)
        {
            titleText.text = skillRow.GetLocalizedName();

            var key = $"SKILL_DESCRIPTION_{skillRow.Id}";

            if (L10nManager.ContainsKey(key))
            {
                SetSkillDescription(key,
                                    skillRow,
                                    optionRow.SkillDamageMin,
                                    optionRow.SkillDamageMax,
                                    optionRow.StatDamageRatioMin,
                                    optionRow.StatDamageRatioMax,
                                    optionRow.SkillChanceMin);
            }
            else
            {
                var digest = new OptionDigest()
                {
                    SkillId = optionRow.SkillId,
                    ChanceMin = optionRow.SkillChanceMin,
                    ChanceMax = optionRow.SkillChanceMax,
                    PowerMin = optionRow.SkillDamageMin,
                    PowerMax = optionRow.SkillDamageMax,
                    StatPowerRatioMin = optionRow.StatDamageRatioMin,
                    StatPowerRatioMax = optionRow.StatDamageRatioMax,
                    ReferencedStatType = optionRow.ReferencedStatType
                };

                switch (skillRow.SkillType)
                {
                    case SkillType.Attack:
                        SetAttackSkillDescription(digest);
                        break;
                    case SkillType.Heal:
                        SetHealDescription(digest);
                        break;
                    case SkillType.Buff:
                        SetBuffDescription(digest, false);
                        break;
                    case SkillType.Debuff:
                        SetBuffDescription(digest, true);
                        break;
                }
            }

            cooldownText.text = $"{L10nManager.Localize("UI_COOLDOWN")}: {skillRow.Cooldown}";
            gameObject.SetActive(true);
        }

        public void Show(SkillSheet.Row skillRow, RuneOptionSheet.Row.RuneOptionInfo optionInfo)
        {
            titleText.text = skillRow.GetLocalizedName();
            var currentValueString = RuneFrontHelper.GetRuneValueString(optionInfo);
            contentText.text = L10nManager.Localize($"SKILL_DESCRIPTION_{skillRow.Id}",
                optionInfo.SkillChance, optionInfo.BuffDuration, currentValueString);
            cooldownText.text = $"{L10nManager.Localize("UI_COOLDOWN")} : {optionInfo.SkillCooldown}";
            buffObject.SetActive(false);
            debuffObject.SetActive(false);
        }

        private void SetSkillDescription(string key, SkillSheet.Row skillRow, long skillPowerMin, long skillPowerMax, int skillRatioMin, int skillRatioMax, int skillChance)
        {
            var sheets = TableSheets.Instance;
            List<string> arg = new List<string>();
            string debuffLimitDisc = string.Empty;
            if (sheets.SkillBuffSheet.TryGetValue(skillRow.Id, out var skillBuffRow))
            {
                var buffList = skillBuffRow.BuffIds;
                if (buffList.Count == 2)
                {
                    var buff = sheets.StatBuffSheet[buffList[0]];
                    var deBuff = sheets.StatBuffSheet[buffList[1]];
                    arg.Add(skillChance.ToString());
                    arg.Add(buff.Duration.ToString());
                    arg.Add((buff.Value + skillPowerMin).ToString());
                    arg.Add(deBuff.Duration.ToString());
                    arg.Add(deBuff.Value.ToString());

                    var buffIcon = BuffHelper.GetStatBuffIcon(buff.StatType, false);
                    buffIconImage.overrideSprite = buffIcon;
                    buffStatTypeText.text = buff.StatType.GetAcronym();

                    var deBuffIcon = BuffHelper.GetStatBuffIcon(deBuff.StatType, true);
                    debuffIconImage.overrideSprite = deBuffIcon;
                    debuffStatTypeText.text = deBuff.StatType.GetAcronym();

                    if (sheets.DeBuffLimitSheet.TryGetValue(deBuff.GroupId, out var debuffLimitRow))
                    {
                        debuffLimitDisc = L10nManager.Localize("SKILL_DESCRIPTION_STATDEBUFF_LIMIT", debuffStatTypeText.text, debuffLimitRow.Value);
                    }

                    buffObject.SetActive(true);
                    debuffObject.SetActive(true);
                }
                else if (buffList.Count == 1)
                {
                    var buff = sheets.StatBuffSheet[buffList[0]];
                    arg.Add(skillChance.ToString());
                    arg.Add(buff.Duration.ToString());
                    arg.Add((buff.Value + skillPowerMin).ToString());
                    buffObject.SetActive(false);
                    debuffObject.SetActive(false);
                    if (skillRow.SkillType == SkillType.Buff)
                    {
                        var buffIcon = BuffHelper.GetStatBuffIcon(buff.StatType, false);
                        buffIconImage.overrideSprite = buffIcon;
                        buffStatTypeText.text = buff.StatType.GetAcronym();
                        buffObject.SetActive(true);
                    }

                    if (skillRow.SkillType == SkillType.Debuff)
                    {
                        var deBuffIcon = BuffHelper.GetStatBuffIcon(buff.StatType, true);
                        debuffIconImage.overrideSprite = deBuffIcon;
                        debuffStatTypeText.text = buff.StatType.GetAcronym();
                        debuffObject.SetActive(true);

                        if (sheets.DeBuffLimitSheet.TryGetValue(buff.GroupId, out var debuffLimitRow))
                        {
                            debuffLimitDisc = L10nManager.Localize("SKILL_DESCRIPTION_STATDEBUFF_LIMIT", debuffStatTypeText.text, debuffLimitRow.Value);
                        }
                    }
                }
                else 
                {
                    buffObject.SetActive(false);
                    debuffObject.SetActive(false);
                }
            }
            else if(sheets.SkillActionBuffSheet.TryGetValue(skillRow.Id, out var skillActionBuffRow))
            {
                arg.Add(skillChance.ToString());
                arg.Add(skillRow.Cooldown.ToString());
                arg.Add(skillPowerMin.ToString());
                var buffIcon = BuffHelper.GetBuffOverrideIcon(skillActionBuffRow.BuffIds.First());
                buffIconImage.overrideSprite = buffIcon;
                buffStatTypeText.text = skillRow.SkillCategory.ToString();
                debuffObject.SetActive(false);
            }
            else if (skillRow.SkillCategory == SkillCategory.ShatterStrike)
            {
                string skilleffect = string.Empty;
                var percentageFormat = new NumberFormatInfo { PercentPositivePattern = 1, PercentNegativePattern = 1 };
                if (skillRatioMin == skillRatioMax)
                {
                    skilleffect = $"{(skillRatioMin / 10000m).ToString("P2", percentageFormat)}";
                }
                else
                {
                    skilleffect = $"{(skillRatioMin / 10000m).ToString("P2", percentageFormat)}~{(skillRatioMax / 10000m).ToString("P2", percentageFormat)}";
                }
                arg.Add(skillChance.ToString());
                arg.Add(skillRow.Cooldown.ToString());
                arg.Add(skilleffect);
                buffObject.SetActive(false);
                debuffObject.SetActive(false);
            }
            else
            {
                string skilleffect = string.Empty;
                if (skillPowerMin == skillPowerMax)
                {
                    skilleffect = skillPowerMin.ToString();
                }
                else
                {
                    skilleffect = $"{skillPowerMin}~{skillPowerMax}";
                }
                arg.Add(skillChance.ToString());
                arg.Add(skillRow.Cooldown.ToString());
                arg.Add(skilleffect);
                buffObject.SetActive(false);
                debuffObject.SetActive(false);
            }

            contentText.text = L10nManager.Localize(key, arg.ToArray()) + debuffLimitDisc;
        }

        public void Show(Skill skill)
        {
            Show(skill.SkillRow,
                skill.Chance, skill.Chance,
                skill.Power, skill.Power,
                skill.StatPowerRatio, skill.StatPowerRatio,
                skill.ReferencedStatType);
        }

        public void Show(
            SkillSheet.Row skillRow,
            int chanceMin,
            int chanceMax,
            long powerMin,
            long powerMax,
            int ratioMin,
            int ratioMax,
            StatType referencedStatType)
        {
            titleText.text = skillRow.GetLocalizedName();

            var key = $"SKILL_DESCRIPTION_{skillRow.Id}";
            if (L10nManager.ContainsKey(key))
            {
                SetSkillDescription(key,
                                    skillRow,
                                    powerMin,
                                    powerMax,
                                    ratioMin,
                                    ratioMax,
                                    chanceMin);
            }
            else
            {
                var digest = new OptionDigest()
                {
                    SkillId = skillRow.Id,
                    ChanceMin = chanceMin,
                    ChanceMax = chanceMax,
                    PowerMin = powerMin,
                    PowerMax = powerMax,
                    StatPowerRatioMin = ratioMin,
                    StatPowerRatioMax = ratioMax,
                    ReferencedStatType = referencedStatType,
                };

                switch (skillRow.SkillType)
                {
                    case SkillType.Attack:
                        SetAttackSkillDescription(digest);
                        break;
                    case SkillType.Heal:
                        SetHealDescription(digest);
                        break;
                    case SkillType.Buff:
                        SetBuffDescription(digest, false);
                        break;
                    case SkillType.Debuff:
                        SetBuffDescription(digest, true);
                        break;
                }
            }

            cooldownText.text = $"{L10nManager.Localize("UI_COOLDOWN")}: {skillRow.Cooldown}";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetAttackSkillDescription(OptionDigest digest)
        {
            var row = TableSheets.Instance.SkillSheet[digest.SkillId];
            SetAttackSkillDescription(row, digest);
        }

        private void SetAttackSkillDescription(SkillSheet.Row row, OptionDigest digest)
        {
            contentText.text = GetDescription("SKILL_DESCRIPTION_ATTACK", row, digest);
            buffObject.SetActive(false);
            debuffObject.SetActive(false);
        }

        private void SetHealDescription(OptionDigest digest)
        {
            var row = TableSheets.Instance.SkillSheet[digest.SkillId];
            SetHealSkillDescription(row, digest);
        }

        private void SetHealSkillDescription(SkillSheet.Row row, OptionDigest digest)
        {
            contentText.text = GetDescription("SKILL_DESCRIPTION_HEAL", row, digest);
            buffObject.SetActive(false);
            debuffObject.SetActive(false);
        }

        private void SetBuffDescription(OptionDigest digest, bool isDebuff)
        {
            var skillRow = TableSheets.Instance.SkillSheet[digest.SkillId];
            SetBuffDescription(
                skillRow,
                digest,
                isDebuff);
        }

        private void SetBuffDescription(SkillSheet.Row skillRow, OptionDigest digest, bool isDebuff)
        {
            var sheets = TableSheets.Instance;
            var buffRow = sheets.StatBuffSheet[sheets.SkillBuffSheet[skillRow.Id].BuffIds.First()];
            var chanceText = digest.ChanceMin == digest.ChanceMax ?
                $"{VariableColorTag}{digest.ChanceMin}%</color>" :
                $"{VariableColorTag}{digest.ChanceMin}-{digest.ChanceMax}%</color>";
            var statType = $"{VariableColorTag}{buffRow.StatType}</color>";

            string desc;
            if (digest.PowerMin == digest.PowerMax &&
                digest.StatPowerRatioMin == digest.StatPowerRatioMax)
            {
                var str = SkillExtensions.EffectToString(
                    skillRow.Id,
                    skillRow.SkillType,
                    digest.PowerMin,
                    digest.StatPowerRatioMin,
                    digest.ReferencedStatType);
                desc = $"{VariableColorTag}{str}</color>";
            }
            else
            {
                var strMin = SkillExtensions.EffectToString(
                    skillRow.Id,
                    skillRow.SkillType,
                    digest.PowerMin,
                    digest.StatPowerRatioMin,
                    digest.ReferencedStatType);
                var strMax = SkillExtensions.EffectToString(
                    skillRow.Id,
                    skillRow.SkillType,
                    digest.PowerMax,
                    digest.StatPowerRatioMax,
                    digest.ReferencedStatType);
                desc = $"{VariableColorTag}{strMin}-{strMax}</color>";
            }

            var value = $"{VariableColorTag}{desc}</color>";

            var icon = BuffHelper.GetStatBuffIcon(buffRow.StatType, isDebuff);
            if (isDebuff)
            {
                debuffStatTypeText.text = buffRow.StatType.GetAcronym();
                debuffIconImage.overrideSprite = icon;

                string debuffLimitDisc = string.Empty;
                if(sheets.DeBuffLimitSheet.TryGetValue(buffRow.GroupId,out var debuffLimitRow))
                {
                    debuffLimitDisc = L10nManager.Localize("SKILL_DESCRIPTION_STATDEBUFF_LIMIT", statType, debuffLimitRow.Value);
                }

                contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_STATDEBUFF", chanceText, statType, value) + debuffLimitDisc;
            }
            else
            {
                buffStatTypeText.text = buffRow.StatType.GetAcronym();
                buffIconImage.overrideSprite = icon;
                contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_STATBUFF", chanceText, statType, value);
            }

            buffObject.SetActive(!isDebuff);
            debuffObject.SetActive(isDebuff);
        }

        private string GetDescription(string format, SkillSheet.Row row, OptionDigest digest)
        {
            var chanceText = digest.ChanceMin == digest.ChanceMax ?
                $"{VariableColorTag}{digest.ChanceMin}%</color>" :
                $"{VariableColorTag}{digest.ChanceMin}-{digest.ChanceMax}%</color>";
            string desc;
            if (digest.PowerMin == digest.PowerMax &&
                digest.StatPowerRatioMin == digest.StatPowerRatioMax)
            {
                var str = SkillExtensions.EffectToString(
                    row.Id,
                    row.SkillType,
                    digest.PowerMin,
                    digest.StatPowerRatioMin,
                    digest.ReferencedStatType);
                desc = $"{VariableColorTag}{str}</color>";
            }
            else
            {
                var strMin = SkillExtensions.EffectToString(
                    row.Id,
                    row.SkillType,
                    digest.PowerMin,
                    digest.StatPowerRatioMin,
                    digest.ReferencedStatType);
                var strMax = SkillExtensions.EffectToString(
                    row.Id,
                    row.SkillType,
                    digest.PowerMax,
                    digest.StatPowerRatioMax,
                    digest.ReferencedStatType);
                desc = $"{VariableColorTag}{strMin}-{strMax}</color>";
            }

            return L10nManager.Localize(format, chanceText, desc);
        }
    }
}
