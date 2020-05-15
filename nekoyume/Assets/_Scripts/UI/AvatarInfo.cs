using Nekoyume.Battle;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AvatarInfo : Widget
    {
        [SerializeField]
        private TextMeshProUGUI nicknameText;

        [SerializeField]
        private TextMeshProUGUI cpText;

        [SerializeField]
        private DetailedStatView[] statViews;


        private const string nicknameTextFormat = "<color=#B38271>Lv.{0}</color=> {1}";

        #region Override

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var gameInstance = Game.Game.instance;
            var player = gameInstance.Stage.selectedPlayer.Model;

            var statTuples = player.Stats.GetBaseAndAdditionalStats();
            var idx = 0;
            foreach (var (statType, value, additionalValue) in statTuples)
            {
                var info = statViews[idx];
                info.Show(statType, value, additionalValue);
                ++idx;
            }

            var currentAvatar = gameInstance.States.CurrentAvatarState;

            nicknameText.text = string.Format(nicknameTextFormat,
                currentAvatar.level,
                currentAvatar.NameWithHash);

            cpText.text = CPHelper.GetCP(currentAvatar, gameInstance.TableSheets.CharacterSheet).ToString();

        }

        #endregion
    }
}
