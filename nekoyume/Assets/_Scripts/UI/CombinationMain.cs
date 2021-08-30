using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationMain : Widget
    {
        [SerializeField] private Button combineButton = null;
        [SerializeField] private Button upgradeButton = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private Image craftNotificationImage = null;
        [SerializeField] private Transform npcPosition = null;

        private NPC _npc = null;
        private const int NPCID = 300001;
        protected override void Awake()
        {
            base.Awake();

            combineButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<Craft>().Show();
            });

            upgradeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<UpgradeEquipment>().Show();
            });

            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            });

            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            craftNotificationImage.enabled = Craft.SharedModel.HasNotification;
            base.Show(ignoreShowAnimation);

            var audioController = AudioController.instance;
            var musicName = AudioController.MusicCode.Combination;
            if (!audioController.CurrentPlayingMusicName.Equals(musicName))
            {
                AudioController.instance.PlayMusic(musicName);
            }

            if (_npc is null)
            {
                var go = Game.Game.instance.Stage.npcFactory.Create(
                    NPCID,
                    npcPosition.position,
                    LayerType.UI,
                    11);
                _npc = go.GetComponent<NPC>();
            }

            NPCShowAnimation();
            HelpPopup.HelpMe(100007, true);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            if (_npc)
            {
                _npc.SpineController.SkeletonAnimation.skeleton.A = 0;
                _npc = null;    
            }
        }

        private void NPCShowAnimation()
        {
            var skeletonTweener = DOTween.To(
                () => _npc.SpineController.SkeletonAnimation.skeleton.A,
                alpha => _npc.SpineController.SkeletonAnimation.skeleton.A = alpha, 1,
                1f);
            var tween = skeletonTweener.Play();
            tween.onComplete += () => _npc.PlayAnimation(NPCAnimation.Type.Greeting_01);
        }
    }
}
