using System.Collections;
using Libplanet;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.State;
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
            var address = new Address();
            var agentAddress = new Address();
            var avatarState = new AvatarState(address, agentAddress, 1);
            var go = Game.instance.stage.playerFactory.Create(avatarState);
            var player = go.GetComponent<Player>();
            var skeleton = player.GetComponentInChildren<SkeletonAnimationController>().SkeletonAnimation.skeleton;
            Assert.AreEqual(1f, skeleton.A);
            player.DoFade(0f, 1f);
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0f, skeleton.A);
        }
    }
}
