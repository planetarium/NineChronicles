using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Battle;
using Nekoyume.Helper;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class RaiderState : IState
    {
        public int TotalScore;
        public int HighScore;
        public int TotalChallengeCount;
        public int RemainChallengeCount;
        public int LatestRewardRank;
        public long ClaimedBlockIndex;
        public long RefillBlockIndex;
        public int PurchaseCount;
        public int Cp;
        public int Level;
        public int IconId;
        public Address AvatarAddress;
        public string AvatarNameWithHash;
        public int LatestBossLevel;

        public RaiderState()
        {
            TotalScore = 0;
            HighScore = 0;
            TotalChallengeCount = 0;
            RemainChallengeCount = WorldBossHelper.MaxChallengeCount;
            AvatarNameWithHash = "";
        }

        public RaiderState(List rawState)
        {
            TotalScore = rawState[0].ToInteger();
            HighScore = rawState[1].ToInteger();
            TotalChallengeCount = rawState[2].ToInteger();
            RemainChallengeCount = rawState[3].ToInteger();
            LatestRewardRank = rawState[4].ToInteger();
            ClaimedBlockIndex = rawState[5].ToLong();
            RefillBlockIndex = rawState[6].ToLong();
            PurchaseCount = rawState[7].ToInteger();
            Cp = rawState[8].ToInteger();
            Level = rawState[9].ToInteger();
            IconId = rawState[10].ToInteger();
            AvatarAddress = rawState[11].ToAddress();
            AvatarNameWithHash = rawState[12].ToDotnetString();
            LatestBossLevel = rawState[13].ToInteger();
        }

        public void Update(AvatarState avatarState, int cp, int score, bool payNcg)
        {
            Level = avatarState.level;
            AvatarAddress = avatarState.address;
            AvatarNameWithHash = avatarState.NameWithHash;
            Cp = cp;
            if (HighScore < score)
            {
                HighScore = score;
            }

            TotalScore += score;
            if (!payNcg)
            {
                RemainChallengeCount--;
            }
            TotalChallengeCount++;
            IconId = avatarState.inventory.GetEquippedFullCostumeOrArmorId();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(TotalScore.Serialize())
                .Add(HighScore.Serialize())
                .Add(TotalChallengeCount.Serialize())
                .Add(RemainChallengeCount.Serialize())
                .Add(LatestRewardRank.Serialize())
                .Add(ClaimedBlockIndex.Serialize())
                .Add(RefillBlockIndex.Serialize())
                .Add(PurchaseCount.Serialize())
                .Add(Cp.Serialize())
                .Add(Level.Serialize())
                .Add(IconId.Serialize())
                .Add(AvatarAddress.Serialize())
                .Add(AvatarNameWithHash.Serialize())
                .Add(LatestBossLevel.Serialize());
        }
    }
}
