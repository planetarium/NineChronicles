using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Craft : Widget
    {
        [SerializeField]
        private Button closeButton = null;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => Close(true));
        }
    }
}
