using Nekoyume.Battle;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
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
        private TextMeshProUGUI cpText = null;

        [SerializeField]
        private ComparisonStatView[] statViews;

        private LevelUpVFX _levelUpVFX = null;

        protected override void PlayPopupSound()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.LevelUp);
        }

        public void Show(int level, bool ignoreShowAnimation = false)
        {
            var gameInstance = Game.Game.instance;
            var currentAvatarState = gameInstance.States.CurrentAvatarState;
            levelText.text = level.ToString();

            var characterSheet = gameInstance.TableSheets.CharacterSheet;
            if (!characterSheet.TryGetValue(currentAvatarState.characterId, out var row))
            {
                throw new System.Exception($"CharacterId{currentAvatarState.characterId} is invaild.");
            }
            cpText.text = CPHelper.GetCP(currentAvatarState, characterSheet).ToString();

            var beforeStat = row.ToStats(level - 1);
            var afterStat = row.ToStats(level);

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
            _levelUpVFX = VFXController.instance.CreateAndChaseCam<LevelUpVFX>(position, new Vector3(0f, 0.7f, 0f));
            _levelUpVFX.Play();
            _levelUpVFX.OnFinished = () => Close();
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
    }
}
