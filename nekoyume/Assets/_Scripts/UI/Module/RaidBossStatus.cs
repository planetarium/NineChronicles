using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Model.Buff;
using Nekoyume.Model;
using Nekoyume.L10n;
using Nekoyume.Helper;
using System;

namespace Nekoyume.UI.Module
{
    public class RaidBossStatus : MonoBehaviour
    {
        [Serializable]
        public struct BossIconInfo
        {
            public int Id;
            public Sprite IconSprite;
        }

        [SerializeField]
        private List<BossIconInfo> iconInfos;

        [SerializeField]
        private Slider hpBar;

        [SerializeField]
        private TextMeshProUGUI hpText;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private Image portrait;

        [SerializeField]
        private BuffLayout buffLayout;

        public void Show()
        {
            gameObject.SetActive(true);
            buffLayout.SetBuff(null);
        }

        public void Close(bool ignoreAnimation = true)
        {
            if (ignoreAnimation)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        public void SetHp(long current, long max)
        {
            current = Math.Max(current, 0);
            hpBar.value = (float) current / max;
            hpText.text = $"{current}/{max}";
        }

        public void SetProfile(Enemy enemy)
        {
            var level = enemy.Level;
            var name = L10nManager.LocalizeCharacterName(enemy.CharacterId);
            var sprite = iconInfos.Find(x => x.Id == enemy.CharacterId).IconSprite;
            SetProfile(level, name, sprite);
            SetHp(enemy.HP, enemy.HP);
        }

        public void SetProfile(int level, string name, Sprite sprite)
        {
            levelText.text = $"Lv.{level}";
            nameText.text = name;
            portrait.overrideSprite = sprite;
        }

        public void SetBuff(Dictionary<int, Buff> modelBuffs)
        {
            buffLayout.SetBuff(modelBuffs);
        }
    }
}
