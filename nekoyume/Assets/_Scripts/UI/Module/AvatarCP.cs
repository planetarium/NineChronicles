using System;
using System.Collections;
using Nekoyume.UI.Tween;
using TMPro;
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
        private GameObject additionalCpArea = null;

        [SerializeField]
        private TextMeshProUGUI additionalCpText = null;

        private Coroutine _disableCpTween;

        private void OnDisable()
        {
            if (!(_disableCpTween is null))
            {
                StopCoroutine(_disableCpTween);
            }
            additionalCpArea.gameObject.SetActive(false);
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
            if (prevCp < currentCp)
            {
                additionalCpArea.gameObject.SetActive(true);
                additionalCpText.text = (currentCp - prevCp).ToString();
                _disableCpTween = StartCoroutine(CoDisableIncreasedCp());
            }
        }

        private IEnumerator CoDisableIncreasedCp()
        {
            yield return new WaitForSeconds(1.5f);
            additionalCpArea.gameObject.SetActive(false);
        }
    }
}
