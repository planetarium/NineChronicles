using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
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

        private LevelUpVFX _levelUpVFX = null;

        private Player _model = null;

        protected override void PlayPopupSound()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
        }

        public void Show(int beforeLevel, int afterLevel, bool ignoreShowAnimation = false)
        {
            var gameInstance = Game.Game.instance;
            _model = gameInstance.Stage.selectedPlayer.Model;
            var characterSheet = gameInstance.TableSheets.CharacterSheet;
            var characterId = _model.CharacterId;
            if (!characterSheet.TryGetValue(characterId, out var row))
            {
                throw new System.Exception($"CharacterId{characterId} is invaild.");
            }
            _model.Level = beforeLevel;
            var previousCP = CPHelper.GetCP(_model);
            cpTextTweener.startValue = previousCP;
            _model.Level = afterLevel;
            var currentCP = CPHelper.GetCP(_model);
            cpTextTweener.endValue = currentCP;

            levelText.text = afterLevel.ToString();
            increasedCpText.text = (currentCP - previousCP).ToString();
            var beforeStat = row.ToStats(beforeLevel);
            var afterStat = row.ToStats(afterLevel);

            using (var enumerator = ((IEnumerable<StatType>) Enum.GetValues(typeof(StatType))).GetEnumerator())
            {
                // StatType.None 스킵
                enumerator.MoveNext();
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
            }

            base.Show(ignoreShowAnimation);

            var position = ActionCamera.instance.transform.position;
            _levelUpVFX = VFXController.instance.CreateAndChaseCam<LevelUpVFX>(position, new Vector3(0f, 0.7f));
            _levelUpVFX.Play();
            _levelUpVFX.OnFinished = () => Close();

            var stage = Game.Game.instance.Stage;
            stage.ReleaseWhiteList.Add(_levelUpVFX.gameObject);
            base.Show(ignoreShowAnimation);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (_levelUpVFX)
            {
                _levelUpVFX = null;
            }

            base.Close(ignoreCloseAnimation);
        }

        private void TweenLevelText()
        {
            cpTextTweener.Play();
        }
    }
}
