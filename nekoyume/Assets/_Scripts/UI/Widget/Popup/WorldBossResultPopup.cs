using Nekoyume.Helper;
using Nekoyume.UI.Module.WorldBoss;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldBossResultPopup : PopupWidget
    {
        [SerializeField]
        private Transform gradeParent;

        [SerializeField]
        private GameObject seasonBestObject;

        [SerializeField]
        private TextMeshProUGUI scoreText;

        private GameObject _gradeObject;

        protected override void OnDisable()
        {
            Destroy(_gradeObject);
            base.OnDisable();
        }

        public void Show(int score, bool isBest)
        {
            base.Show();

            scoreText.text = score.ToString("N0");
            seasonBestObject.SetActive(isBest);
            var grade = (WorldBossGrade) WorldBossHelper.CalculateRank(score);

            if (WorldBossFrontHelper.TryGetGrade(grade, out var prefab))
            {
                _gradeObject = Instantiate(prefab, gradeParent);
                _gradeObject.transform.localScale = Vector3.one;
            }
        }
    }
}
