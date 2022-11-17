using System.Collections;
using Nekoyume.UI.Tween;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class AvatarCP : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI cpText = null;

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

        private Coroutine _disableCpTween;

        private void OnDisable()
        {
            if (!(_disableCpTween is null))
            {
                StopCoroutine(_disableCpTween);
            }

            increaseCpArea.gameObject.SetActive(false);
        }

        public void UpdateCP(int cp)
        {
            cpText.text = cp.ToString();
        }

        public void PlayAnimation(int prevCp, int currentCp)
        {
            if (!(_disableCpTween is null))
            {
                StopCoroutine(_disableCpTween);
            }

            cpTextValueTweener.Play(prevCp, currentCp);
            increaseCpArea.gameObject.SetActive(false);
            decreaseCpArea.gameObject.SetActive(false);
            if (prevCp < currentCp)
            {
                increaseCpArea.gameObject.SetActive(true);
                additionalCpText.text = (currentCp - prevCp).ToString();
            }
            else if (prevCp > currentCp)
            {
                decreaseCpArea.gameObject.SetActive(true);
                decreaseCpText.text = (math.abs(currentCp - prevCp)).ToString();
            }

            _disableCpTween = StartCoroutine(CoDisableAdditionalCp());
        }

        private IEnumerator CoDisableAdditionalCp()
        {
            yield return new WaitForSeconds(1.5f);
            increaseCpArea.gameObject.SetActive(false);
            decreaseCpArea.gameObject.SetActive(false);
        }
    }
}
