using System.Collections;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.State;
using Nekoyume.UI;
using NUnit.Framework;
using Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class PlayerTest : PlayModeTest
    {
        [UnityTest]
        public IEnumerator DoFade()
        {
            miner = new MinerFixture("player_doFade");

            // CreateAvatar
            Widget.Find<Title>().OnClick();
            Widget.Find<Synopsis>().End();
            yield return new WaitUntil(() => Widget.Find<Login>().ready);
            Widget.Find<Login>().SlotClick(2);
            var loginDetail = Widget.Find<LoginDetail>();
            loginDetail.CreateAndLogin("DoFade");
            yield return new WaitUntil(() => Game.instance.Agent.StagedTransactions.Any());
            var createAvatarTx = Game.instance.Agent.StagedTransactions.First();
            yield return miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.CurrentAvatarState is null);

            var player = Game.instance.Stage.GetPlayer();
            var skeleton = player.GetComponentInChildren<SkeletonAnimationController>().SkeletonAnimation.skeleton;
            Assert.AreEqual(1f, skeleton.A);
            player.DoFade(0f, 1f);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0f, skeleton.A);
        }
    }
}
