using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class LevelUpCelebratePopup : PopupWidget
    {
        [SerializeField]
        private TextMeshProUGUI levelText = null;

        [SerializeField]
        private DigitTextTweener cpTextTweener = null;

        [SerializeField]
        private TextMeshProUGUI increasedCpText = null;

        [SerializeField]
        private ComparisonStatView[] statViews = null;

        private Player _model = null;

        private readonly StatType[] StatTypes = new StatType[]
        {
            StatType.HP,
            StatType.ATK,
            StatType.DEF,
            StatType.CRI,
            StatType.HIT,
            StatType.SPD,
        };

        protected override void PlayPopupSound()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
        }

        public void Show(int beforeLevel, int afterLevel, bool ignoreShowAnimation = false)
        {
            var gameInstance = Game.Game.instance;
            _model = gameInstance.Stage.SelectedPlayer.Model;
            var characterSheet = gameInstance.TableSheets.CharacterSheet;
            var characterId = _model.CharacterId;
            var costumeStatSheet = gameInstance.TableSheets.CostumeStatSheet;
            if (!characterSheet.TryGetValue(characterId, out var row))
            {
                throw new System.Exception($"CharacterId{characterId} is invaild.");
            }
            _model.Level = beforeLevel;
            var previousCP = CPHelper.GetCP(_model, costumeStatSheet);
            cpTextTweener.beginValue = previousCP;
            _model.Level = afterLevel;
            var currentCP = CPHelper.GetCP(_model, costumeStatSheet);
            cpTextTweener.endValue = currentCP;

            levelText.text = afterLevel.ToString();
            increasedCpText.text = (currentCP - previousCP).ToString();
            var beforeStat = row.ToStats(beforeLevel);
            var afterStat = row.ToStats(afterLevel);

            var enumerator = ((IEnumerable<StatType>)StatTypes).GetEnumerator();
            foreach (var view in statViews)
            {
                if (!enumerator.MoveNext())
                {
                    view.gameObject.SetActive(false);
                    continue;
                }

                var type = enumerator.Current;

                view.Show(type,
                    beforeStat.GetStat(type, true),
                    afterStat.GetStat(type, true));
                view.gameObject.SetActive(true);
            }

            base.Show(ignoreShowAnimation);

            var position = ActionCamera.instance.transform.position;

            var stage = Game.Game.instance.Stage;
            base.Show(ignoreShowAnimation);
        }

        private void TweenLevelText()
        {
            cpTextTweener.Play();
        }
    }
}
