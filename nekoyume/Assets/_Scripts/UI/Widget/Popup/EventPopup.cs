using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class EventPopup : PopupWidget
    {
        [field: SerializeField]
        public EventView EventView { get; private set; }
    }
}
