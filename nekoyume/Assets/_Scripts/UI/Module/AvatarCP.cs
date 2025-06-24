using System.Collections;
using Nekoyume.Helper;
using Nekoyume.UI.Tween;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class AvatarCP : MonoBehaviour
    {
        [SerializeField]
        private DigitTextTweener cpTextValueTweener = null;

        [SerializeField]
        private GameObject increaseCpArea = null;

        [SerializeField]
        private GameObject decreaseCpArea = null;

        [SerializeField]
        private TextMeshProUGUI additionalCpText = null;

        [SerializeField]
        private TextMeshProUGUI decreaseCpText = null;

        public void PlayAnimation(int prevCp, int currentCp)
        {
            cpTextValueTweener.Play(prevCp, currentCp);
            increaseCpArea.gameObject.SetActive(false);
            decreaseCpArea.gameObject.SetActive(false);
            if (prevCp < currentCp)
            {
                increaseCpArea.gameObject.SetActive(true);
                additionalCpText.text = TextHelper.FormatNumber(currentCp - prevCp);
            }
            else if (prevCp > currentCp)
            {
                decreaseCpArea.gameObject.SetActive(true);
                decreaseCpText.text = TextHelper.FormatNumber(math.abs(currentCp - prevCp));
            }
        }
    }
}
