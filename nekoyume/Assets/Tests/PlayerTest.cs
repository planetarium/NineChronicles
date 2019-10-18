using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
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
            loginDetail.nameField.text = "doFade";
            loginDetail.CreateClick();
            yield return new WaitUntil(() => Game.instance.agent.StagedTransactions.Any());
            var createAvatarTx = Game.instance.agent.StagedTransactions.First();
            yield return miner.CoMine(createAvatarTx);
            yield return new WaitWhile(() => States.Instance.CurrentAvatarState.Value is null);

            var player = Game.instance.stage.GetPlayer();
            var skeleton = player.GetComponentInChildren<SkeletonAnimationController>().SkeletonAnimation.skeleton;
            Assert.AreEqual(1f, skeleton.A);
            player.DoFade(0f, 1f);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0f, skeleton.A);
        }
    }
}
