#nullable enable

using Nekoyume.Model.BattleStatus;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nekoyume.Game.Battle
{
    // TODO: HackAndSlash, EventDungeon, Raid, Area 등 공통 로직 처리
    public class BattleRenderer
    {
        #region Singleton
        private static class Singleton
        {
            internal static readonly BattleRenderer Value = new();
        }

        public static BattleRenderer Instance => Singleton.Value;
        #endregion Singleton

        #region Fields
        private Action<BattleLog>? _onPrepareStage;
        private Action<BattleLog>? _onStageStart;
        #endregion Fields

        #region Properties & Events

        // TODO: don't use public setter
        // TODO: UI코드에서 불리는 부분이 많은데, 다른 방식으로 체크 불가능?
        // BattlePreparation쪽에서도 set되고, battle쪽에서도 set되는데 중복처리 아닌지
        // 캐릭터 머리 위 UI도 체크해서 고치자
        public bool IsOnBattle { get; set; }

        /// <summary>
        /// ActionRenderHandler의 응답을 받아 렌더링할 리소르를 준비할 때 호출
        /// </summary>
        public event Action<BattleLog>? OnPrepareStage
        {
            add
            {
                _onPrepareStage -= value;
                _onPrepareStage += value;
            }
            remove => _onPrepareStage -= value;
        }

        /// <summary>
        /// OnPrepareStage호출 후 리소스가 준비되어 전투가 시작될 때 호출
        /// </summary>
        public event Action<BattleLog>? OnStageStart
        {
            add
            {
                _onStageStart -= value;
                _onStageStart += value;
            }
            remove => _onStageStart -= value;
        }
        #endregion Properties & Events

        // 나무이동, 에셋로드, 기타등등 대기..

        // Stage, StageLoadingEffect등등.. 확인

        public void PrepareStage(BattleLog battleLog)
        {
            _onPrepareStage?.Invoke(battleLog);
        }

        #region AssetLoad
        // TODO: 씬 분리 후 제거
        private readonly HashSet<int> loadedMonsterIds = new();

        // TODO: VFX도 처리

        public IEnumerator LoadStageResources(BattleLog battleLog)
        {
            ReleaseMonsterResources();
            yield return LoadMonsterResources(battleLog.GetMonsterIds());
            _onStageStart?.Invoke(battleLog);
        }

        // TODO: 필요한 것만 로드
        private IEnumerator LoadMonsterResources(HashSet<int> monsterIds)
        {
            var resourceManager = ResourceManager.Instance;
            foreach (var monsterId in monsterIds)
            {
                yield return resourceManager.LoadAsync<GameObject>(monsterId.ToString()).ToCoroutine();
                loadedMonsterIds.Add(monsterId);
            }
        }

        public void ReleaseMonsterResources()
        {
            var resourceManager = ResourceManager.Instance;
            foreach (var loadedMonsterId in loadedMonsterIds)
            {
                resourceManager.Release(loadedMonsterId.ToString());
            }
            loadedMonsterIds.Clear();
        }
        #endregion AssetLoad
    }
}
