using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model;

namespace Nekoyume.State
{
    /// <summary>
    /// Agent가 포함하는 각 Avatar의 데이터 모델.
    /// </summary>
    [Serializable]
    public class AvatarState
    {
        public Avatar avatar;
        public BattleLog battleLog;
        public decimal gold;
        public DateTimeOffset updatedAt;
        public DateTimeOffset? clearedAt;
        public Address? AvatarAddress { get; private set; }

        // ToDo. 각 액션의 변경점을 담을 수 있는 변수가 필요합니다.
        // 우선 SellRenew 액션에 한해서 이 방법을 적용해보고 있습니다.
        // 액션을 등록하고 해당 액션 타입의 결과를 쌓아 1회에 한해 꺼내갈 수 있는 변수입니다.
        // ToDo. 각 액션 내에 결과 값을 저장 시키는 식의 접근도 고려해봐야 합니다.
        private readonly Dictionary<string, GameActionResult> _gameActionResults = new Dictionary<string, GameActionResult>();

        public AvatarState(Avatar avatar, Address? address, BattleLog logs = null, int gold = 0)
        {
            this.avatar = avatar;
            battleLog = logs;
            this.gold = gold;
            updatedAt = DateTimeOffset.UtcNow;
            AvatarAddress = address;
        }

        public void SetGameActionResult(GameActionResult result)
        {
            var key = result.GetType().Name;
            if (!_gameActionResults.ContainsKey(key))
            {
                _gameActionResults.Add(key, result);
                return;
            }
            
            _gameActionResults[key] = result;
        }
        
        public T GetGameActionResult<T>() where T : GameActionResult
        {
            var key = typeof(T).Name;
            if (!_gameActionResults.ContainsKey(key))
            {
                return null;
            }

            var result = _gameActionResults[key];
            
            // ToDo. 한 번만 빼갈 수 있도록 할지, 다음 할당까지는 유지시킬지 생각해봐야 합니다.
            // 기본적으로는 전자입니다.
            // 데이타 단일성 원칙을 책임지는 객체가 일괄적으로 가져가는 형태가 좋다고 생각합니다.
            // 비록 지금은 각 액션을 사용하는 측에서 가져가지만..
            _gameActionResults[key] = null;
            return (T)result;
        }
    }
}
