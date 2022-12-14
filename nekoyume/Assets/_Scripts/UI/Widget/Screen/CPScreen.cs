using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class CPScreen : ScreenWidget
    {
        [SerializeField]
        private AvatarCP cp;

        private static readonly int HashToShow = Animator.StringToHash("Show");

        public void Show(int prevCp, int currentCp)
        {
            base.Show(true);
            Animator.Play(HashToShow);
            cp.PlayAnimation(prevCp, currentCp);
        }
    }
}
