#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif
#if !UNITY_EDITOR && UNITY_STANDALONE
#define RUN_ON_STANDALONE
#endif
//#define TEST_LOG

using System;
using System.Collections;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;

namespace Nekoyume.Game.Battle
{
    public partial class Stage
    {
        class SkipStageEvent : EventBase
        {
            public SkipStageEvent(CharacterBase character) : base(character)
            {
            }

            public override IEnumerator CoExecute(IStage stage)
            {
                return stage.CoCustomEvent(Character, this);
            }
        }
    }
}
