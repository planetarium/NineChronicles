using Nekoyume.UI;
using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AdventureBossRewardInfoPopup : PopupWidget
    {
        [SerializeField] private UnityEngine.UI.ToggleGroup toggleGroup;
        [SerializeField] private GameObject contentsScore;
        [SerializeField] private GameObject contentsFloor;
        [SerializeField] private GameObject contentsOperational;
    }
}
