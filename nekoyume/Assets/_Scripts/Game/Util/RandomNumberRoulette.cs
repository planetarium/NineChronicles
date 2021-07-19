using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Game.Util
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class RandomNumberRoulette : MonoBehaviour
    {
        [SerializeField] private bool playOnEnable;
        [SerializeField] private int digit;
        [SerializeField] private float intervalTime;

        private TextMeshProUGUI _number;
        private Coroutine _rouletteCoroutine;
        private bool _isPlaying;

        private void Awake()
        {
            _number = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (!playOnEnable)
            {
                return;
            }

            Play();
        }

        public void Play()
        {
            if (!isActiveAndEnabled || _isPlaying)
            {
                return;
            }

            if (_rouletteCoroutine != null)
            {
                StopCoroutine(_rouletteCoroutine);
            }

            _rouletteCoroutine = StartCoroutine(Play(_number, digit, intervalTime));
            _isPlaying = true;
        }

        public void Stop()
        {
            if (!isActiveAndEnabled || !_isPlaying)
            {
                return;
            }

            if (_rouletteCoroutine != null)
            {
                StopCoroutine(_rouletteCoroutine);
            }

            var sb = new StringBuilder();
            for (var i = 0; i < digit; i++)
            {
                sb.Append(0);
            }
            _number.text = sb.ToString();
            _isPlaying = false;
        }

        private IEnumerator Play(TMP_Text effect, int digit, float intervalTime)
        {
            var sb = new StringBuilder();
            while (true)
            {
                sb.Length = 0;
                for (var i = 0; i < digit; i++)
                {
                    sb.Append(Random.Range(0, 10));
                }

                effect.text = sb.ToString();
                yield return new WaitForSeconds(intervalTime);
            }
        }
    }
}
