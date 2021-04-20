using Libplanet;
using Nekoyume.Model.State;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RankCell : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI rankText = null;

        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField]
        private TextMeshProUGUI addressText = null;

        [SerializeField]
        private TextMeshProUGUI firstElement = null;

        [SerializeField]
        private TextMeshProUGUI secondElement = null;

        public void SetDataAsAbility(int rank, string nickname, Address avatarAddress, int level, int cp)
        {
            rankText.text = rank.ToString();
            nicknameText.text = nickname;
            addressText.text = avatarAddress.ToString();
            firstElement.text = level.ToString();
            secondElement.text = cp.ToString();

            gameObject.SetActive(true);
        }
    }
}
