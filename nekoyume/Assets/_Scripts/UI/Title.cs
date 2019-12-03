using Nekoyume.Game.Controller;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Title : Widget
    {
        public bool ready = false;
        public Animator animator;

        public override void Show()
        {
            base.Show();
            animator.enabled = false;
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
        }

        public void OnClick()
        {
            if (!ready)
                return;

            Find<Synopsis>().Show();
            Close();
        }

        public void Ready()
        {
            ready = true;
            animator.enabled = true;
        }
    }
}
