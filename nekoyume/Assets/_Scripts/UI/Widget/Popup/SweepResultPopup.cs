using System.Collections;
using System.Collections.Generic;
using Libplanet.Action;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class SweepResultPopup : PopupWidget
    {
        [SerializeField]
        private TimeMachineRewind sweepRewind;

        [SerializeField]
        private TimeMachineRewind loadingRewind;

        [SerializeField]
        private PlayableDirector playableDirector;

        [SerializeField]
        private ConditionalButton okButton;

        [SerializeField]
        private ConditionalButton mainButton;

        [SerializeField]
        private SkeletonGraphic spine;

        [SerializeField]
        private Animator background;

        [SerializeField]
        private Image playCountBar;

        [SerializeField]
        private TextMeshProUGUI playCountText;

        [SerializeField]
        private TextMeshProUGUI stageText;

        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private Transform titleDecoContainer;

        [SerializeField]
        private List<SweepItem> items;

        [SerializeField]
        private List<GameObject> questions;

        [SerializeField]
        private AnimationCurve animationCurve;

        [SerializeField]
        private float defaultSpeed = 1;

        [SerializeField]
        private float maxSpeed = 5;

        [SerializeField]
        private float maxTime = 20;

        [SerializeField]
        private float delayTime = 3;

        [SerializeField]
        private int maxPlayCount = 11;

        [SerializeField]
        private GameObject[] seasonPassObjs;

        [SerializeField]
        private TextMeshProUGUI seasonPassCourageAmount;

        private GameObject _titleDeco;
        private Coroutine _coroutine;
        private StageSheet.Row _stageRow;
        private float _timeElapsed;
        private int _apPlayCount = 0;
        private int _apStonePlayCount = 0;
        private int _fixedApStonePlayCount = 0;

        private readonly ReactiveProperty<int> _attackCount = new ReactiveProperty<int>();
        private readonly ReactiveProperty<bool> _sweepRewind = new ReactiveProperty<bool>(true);

        private void Start()
        {
            _attackCount.Subscribe(UpdatePlayCount).AddTo(gameObject);
            _sweepRewind.Subscribe(UpdateSweep).AddTo(gameObject);
            okButton.Text = L10nManager.Localize("UI_OK");
            okButton.OnSubmitSubject.Subscribe(_ => Close()).AddTo(gameObject);

            mainButton.Text = L10nManager.Localize("UI_MAIN");
            mainButton.OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                Find<BattlePreparation>().Close();
                Find<StageInformation>().Close();
                Find<WorldMap>().Close();
                Game.Event.OnRoomEnter.Invoke(true);
            }).AddTo(gameObject);
        }

        public void Show(StageSheet.Row stageRow, int worldId,
            int apPlayCount, int apStonePlayCount,
            long exp, bool ignoreShowAnimation = false)
        {
            _stageRow = stageRow;
            _fixedApStonePlayCount = States.Instance.GameConfigState.ActionPointMax / stageRow.CostAP;
            _apPlayCount = apPlayCount;
            _apStonePlayCount = apStonePlayCount;

            loadingRewind.IsRunning = true;
            stageText.text = $"STAGE {stageRow.Id}";
            expText.text = $"EXP + {exp}";
            UpdateTitleDeco(worldId);

            base.Show(ignoreShowAnimation);

            for (var i = 0; i < questions.Count; i++)
            {
                var isActive = i < stageRow.Rewards.Count;
                questions[i].SetActive(isActive);
            }

            _attackCount.SetValueAndForceNotify(0);
            _sweepRewind.SetValueAndForceNotify(true);
            playableDirector.Play();

            RefreshSeasonPassCourageAmount(apPlayCount + apStonePlayCount);
        }

        private void UpdateTitleDeco(int worldId)
        {
            if (_titleDeco)
            {
                Destroy(_titleDeco);
            }

            var cutscenePath = $"UI/Prefabs/UI_WorldClear_{worldId:D2}";
            NcDebug.Log($"cutscenePath :{cutscenePath}");
            var clone = Resources.Load<GameObject>(cutscenePath) ??
                        Resources.Load<GameObject>("UI/Prefabs/UI_WorldClear_01");
            _titleDeco = Instantiate(clone, titleDecoContainer);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            StopSweep();
            AudioController.instance.PlayMusic(AudioController.MusicCode.Main, 2.0f);
            base.Close(ignoreCloseAnimation);
        }

        public void OnActionRender(IRandom rand)
        {
            if (_stageRow is null)
            {
                return;
            }

            var materialSheet = Game.Game.instance.TableSheets.MaterialItemSheet;
            var rewards = Action.HackAndSlashSweep.GetRewardItems(rand,
                _apPlayCount + _apStonePlayCount, _stageRow, materialSheet);

            var bundle = new Dictionary<ItemBase, int>();
            foreach (var itemBase in rewards)
            {
                if (bundle.ContainsKey(itemBase))
                {
                    bundle[itemBase] += 1;
                }
                else
                {
                    bundle.Add(itemBase, 1);
                }
            }

            foreach (var item in items)
            {
                item.gameObject.SetActive(false);
            }

            var index = 0;
            foreach (var pair in bundle)
            {
                items[index].gameObject.SetActive(true);
                items[index].Set(pair.Key, pair.Value);
                index++;
            }

            loadingRewind.IsRunning = false;
        }

        private void UpdatePlayCount(int attackCount)
        {
            var max = _apStonePlayCount / _fixedApStonePlayCount;
            if (_apPlayCount > 0)
            {
                max += 1;
            }
            var attackMaxCount = Mathf.Clamp(max, 1, maxPlayCount);
            playCountBar.fillAmount = (float)attackCount / attackMaxCount;

            var curPlayCount = Mathf.Min(attackCount * _fixedApStonePlayCount, _apPlayCount + _apStonePlayCount);
            playCountText.text = $"{curPlayCount}/{_apPlayCount + _apStonePlayCount}";

            if (attackCount >= attackMaxCount)
            {
                _sweepRewind.SetValueAndForceNotify(false);
            }
        }

        private void UpdateSweep(bool value)
        {
            StopSweep();

            sweepRewind.IsRunning = value;
            if (value)
            {
                _coroutine = StartCoroutine(Accelerate());
            }
            else
            {
                ApplySpeed(defaultSpeed);
            }
        }

        private void StopSweep()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
        }

        private IEnumerator Accelerate()
        {
            _timeElapsed = 0;
            _timeElapsed -= delayTime;
            while (_timeElapsed < maxTime)
            {
                _timeElapsed += Time.deltaTime;
                if (_timeElapsed > 0)
                {
                    var value = animationCurve.Evaluate(_timeElapsed / maxTime) * maxSpeed;
                    var speed = Mathf.Clamp(value, defaultSpeed, maxSpeed);
                    ApplySpeed(speed);
                }

                yield return null;
            }
        }

        private void ApplySpeed(float speed)
        {
            playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(speed);
            background.speed = speed;
        }

        public void OnBattleFinish() // for signal receiver
        {
            _attackCount.Value += 1;
        }

        public void OnStopMusic()
        {
            AudioController.instance.StopMusicAll();
        }

        private void RefreshSeasonPassCourageAmount(int playCount)
        {
            if (Game.Game.instance.SeasonPassServiceManager.CurrentSeasonPassData != null)
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(true);
                }
                seasonPassCourageAmount.text = $"+{Game.Game.instance.SeasonPassServiceManager.AdventureSweepCourageAmount * playCount}";
            }
            else
            {
                foreach (var item in seasonPassObjs)
                {
                    item.SetActive(false);
                }
            }
        }
    }
}
