using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Craft : Widget
    {
        [field: SerializeField]
        private Button CloseButton { get; set; } = null;

        protected override void Awake()
        {
            base.Awake();
            CloseButton.onClick.AddListener(() => Close(true));
        }
    }
}
