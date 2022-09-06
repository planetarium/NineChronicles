using System;
using Bencodex.Types;
using Libplanet;
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
        public string AvatarName;
        public int LatestBossLevel;
        public long UpdatedBlockIndex;

        public RaiderState()
        {
            TotalScore = 0;
            HighScore = 0;
            TotalChallengeCount = 0;
            RemainChallengeCount = WorldBossHelper.MaxChallengeCount;
            AvatarName = "";
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
            AvatarName = rawState[12].ToDotnetString();
            LatestBossLevel = rawState[13].ToInteger();
            UpdatedBlockIndex = rawState[14].ToLong();
        }

        public void Update(AvatarState avatarState, int cp, int score, bool payNcg, long blockIndex)
        {
            Level = avatarState.level;
            AvatarAddress = avatarState.address;
            AvatarName = avatarState.name;
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
            UpdatedBlockIndex = blockIndex;
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
                .Add(AvatarName.Serialize())
                .Add(LatestBossLevel.Serialize())
                .Add(UpdatedBlockIndex.Serialize());
        }
    }
}
