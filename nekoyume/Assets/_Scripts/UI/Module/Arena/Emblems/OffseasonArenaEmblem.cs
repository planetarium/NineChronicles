using UnityEngine;

namespace Nekoyume.UI.Module.Arena.Emblems
{
    public class OffseasonArenaEmblem : MonoBehaviour
    {
        public void Show(int seasonNumber, bool isNormal)
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}