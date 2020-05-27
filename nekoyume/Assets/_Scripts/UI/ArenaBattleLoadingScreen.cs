using DG.Tweening;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ArenaBattleLoadingScreen : ScreenWidget
    {
        public CharacterProfile playerProfile;
        public CharacterProfile enemyProfile;
        public RectTransform playerMoveImage;
        public RectTransform enemyMoveImage;
        public RectTransform backPlateRight;
        public RectTransform backPlateLeft;
        public RectTransform emblem;
        public RectTransform versus;
        public RectTransform versusEffect;
        public TextMeshProUGUI loadingText;

        [Space]
        [Header(" 애니메이션 셋팅 ")]

        [Tooltip("뮨이 열리고 닫힐때 걸리는 타이밍")]
        public float backPlateAnimatingAt;
        [Tooltip("뮨이 열리고 닫힐때 걸리는 시간")]
        public float backPlateAnimatingTime;
        [Tooltip("앰블렘이 닫히로 열리는 타이밍")]
        public float emblemAnimatingAt;
        [Tooltip("앰블렘이 닫히로 열리는 시간")]
        public float emblemAnimatingTime;
        [Tooltip("플레이어 와 적 표시 칸이 내려오는데 걸리는 타이밍")]
        public float playerProfileAnimatingAt;
        [Tooltip("플레이어 와 적 표시 칸이 내려오는데 걸리는 시간")]
        public float playerProfileAnimatingTime;
        [Tooltip("플레이어 와 적 표시 칸이 내려오는데 걸리는 타이밍")]
        public float versusAnimatingAt;
        [Tooltip("플레이어 와 적 표시 칸이 내려오는데 걸리는 시간")]
        public float versusAnimatingTime;
        [Tooltip("로딩 텍스트 애니메이션 실행 타이밍")]
        public float loadingTextAnimatingAt;
        [Tooltip("로딩 텍스트 애니메이션 시간")]
        public float loadingTextAnimatingTime;
        [Tooltip("대기중 흔들리는 효과 범위")]
        public float shakeRadius;


        private Sequence introSequence;
        private Sequence waitSequence;

        public void Show(ArenaInfo enemyInfo)
        {
            var player = Game.Game.instance.Stage.GetPlayer();
            var sprite = SpriteHelper.GetItemIcon(player.Model.armor?.Data.Id ?? GameConfig.DefaultAvatarArmorId);
            playerProfile.Set(player.Level, States.Instance.CurrentAvatarState.NameWithHash, sprite);
            var enemySprite = SpriteHelper.GetItemIcon(enemyInfo.ArmorId);
            enemyProfile.Set(enemyInfo.Level, enemyInfo.AvatarName, enemySprite);
            Show();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            ResetAnimation();

            Observable.NextFrame().Subscribe(unit =>
            {
                introSequence.Rewind();
                introSequence.Restart();
            });
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            waitSequence.Rewind();
            introSequence.PlayBackwards();

            base.Close(ignoreCloseAnimation);
        }

        private void ResetAnimation()
        {
            backPlateRight.anchoredPosition = new Vector2(586, 0);
            backPlateLeft.anchoredPosition = new Vector2(-586, 0);

            emblem.anchoredPosition = new Vector2(0, 336);

            playerMoveImage.anchoredPosition = new Vector2(-358, 345);

            enemyMoveImage.anchoredPosition = new Vector2(358, -345);
        }

        private void Start()
        {
            introSequence = DOTween.Sequence();

            var leftBackPlateSeq = backPlateLeft.DOLocalMoveX(0, backPlateAnimatingTime, true);
            var rightBackPlateSeq = backPlateRight.DOLocalMoveX(0, backPlateAnimatingTime, true);

            var emblemSeq = emblem.DOLocalMoveY(0, emblemAnimatingTime, true);

            var playerMoveSeq = DOTween.Sequence()
                .Append(playerMoveImage.GetComponent<Image>().DOFade(0, 0))
                .Join(playerProfile.GetComponent<CanvasGroup>().DOFade(0, 0))
                .Insert(playerProfileAnimatingAt, playerMoveImage.DOLocalMoveY(0, playerProfileAnimatingTime, true))
                .Join(playerMoveImage.GetComponent<Image>().DOFade(1, 0.02f).From(0))
                .Append(playerMoveImage.GetComponent<Image>().DOFade(0, 0.2f))
                .Join(playerProfile.GetComponent<CanvasGroup>().DOFade(1, 0.2f));

            var enemyMoveSeq = DOTween.Sequence()
                .Append(enemyMoveImage.GetComponent<Image>().DOFade(0, 0))
                .Join(enemyProfile.GetComponent<CanvasGroup>().DOFade(0, 0))
                .Insert(playerProfileAnimatingAt, enemyMoveImage.DOLocalMoveY(0, playerProfileAnimatingTime, true))
                .Join(enemyMoveImage.GetComponent<Image>().DOFade(1, 0.02f).From(0))
                .Append(enemyMoveImage.GetComponent<Image>().DOFade(0, 0.2f))
                .Join(enemyProfile.GetComponent<CanvasGroup>().DOFade(1, 0.2f));

            var versusSeq = DOTween.Sequence()
                .Append(versusEffect.GetComponent<Image>().DOFade(0, 0))
                .Insert(versusAnimatingAt, versus.DOScale(Vector3.one, versusAnimatingTime).From(Vector3.one * 1.4f))
                .Join(versus.GetComponent<Image>().DOFade(1, versusAnimatingTime).From(0))
                .Append(versusEffect.GetComponent<Image>().DOFade(1, 0.2f));

            var loadingTextSeq = loadingText.DOFade(1, loadingTextAnimatingTime).From(0);

            introSequence.SetAutoKill(false)
                .Insert(backPlateAnimatingAt, leftBackPlateSeq)
                .Insert(backPlateAnimatingAt, rightBackPlateSeq)
                .Insert(emblemAnimatingAt, emblemSeq)
                .Insert(0, playerMoveSeq)
                .Insert(0, enemyMoveSeq)
                .Insert(0, versusSeq)
                .Insert(loadingTextAnimatingAt, loadingTextSeq);
            introSequence.onComplete += () => waitSequence.Restart();

            introSequence.Pause();

            waitSequence = DOTween.Sequence();

            var leftShake = backPlateLeft.DOShakeAnchorPos(2, shakeRadius, 4);
            var rightShake = backPlateRight.DOShakeAnchorPos(2, shakeRadius, 4);
            var emblemShake = emblem.DOShakeAnchorPos(2, shakeRadius, 4);
            var playerMoveImageShake = playerMoveImage.DOShakeAnchorPos(2, shakeRadius, 4);
            var enemyMoveImageShake = enemyMoveImage.DOShakeAnchorPos(2, shakeRadius, 4);
            var playerProfileShake = playerProfile.GetComponent<RectTransform>().DOShakeAnchorPos(1, shakeRadius, 4);
            var enemyProfileShake = enemyProfile.GetComponent<RectTransform>().DOShakeAnchorPos(1, shakeRadius, 4);

            waitSequence.SetAutoKill(false)
                .Append(leftShake).Join(rightShake).Join(emblemShake).Join(playerMoveImageShake)
                .Join(enemyMoveImageShake).Join(playerProfileShake).Join(enemyProfileShake);

            waitSequence.Pause();
            waitSequence.SetLoops(-1, LoopType.Restart);

            CloseWidget = null;
            SubmitWidget = null;
        }
    }
}
