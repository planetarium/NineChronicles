using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Game.Util
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class RandomNumberRoulette : MonoBehaviour
    {
        [SerializeField] private int digit;
        [SerializeField] private float cooltime;

        private TextMeshProUGUI _number;
        private Coroutine _rouletteCoroutine;

        private void Awake()
        {
            _number = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (_rouletteCoroutine != null)
            {
                StopCoroutine(_rouletteCoroutine);
            }

            _rouletteCoroutine = StartCoroutine(Play(_number, digit, cooltime));
        }

        public static IEnumerator Play(TMP_Text effect, int digit, float time)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                sb.Length = 0;
                for (int i = 0; i < digit; i++)
                {
                    sb.Append(Random.Range(0, 10));
                }
                effect.text = sb.ToString();
                yield return new WaitForSeconds(time);
            }
        }
    }
}
