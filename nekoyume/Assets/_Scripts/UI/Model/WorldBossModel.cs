using System;
using System.Collections.Generic;
using Nekoyume.UI.Module.Lobby;

namespace Nekoyume.UI.Model
{
    public class WorldBossRankingRecord
    {
        public int Ranking;
        public int Level;
        public int Cp;
        public int IconId;
        public string AvatarName;
        public string Address;
        public int HighScore;
        public int TotalScore;
    }

    public class worldBossRanking
    {
        public long BlockIndex;
        public List<WorldBossRankingRecord> RankingInfo;
    }

    public class WorldBossRankingResponse
    {
        public int WorldBossTotalUsers;
        public worldBossRanking WorldBossRanking;
    }

    [Serializable]
    public class TxResultResponse
    {
        public Transaction transaction;
    }

    [Serializable]
    public class Transaction
    {
        public TransactionResult transactionResult;
    }

    [Serializable]
    public class TransactionResult
    {
        public TxStatus txStatus;
    }

    public class GoldBalanceResponse
    {
        public string balance;
    }

    [Serializable]
    public enum TxStatus {
        INVALID,
        STAGING,
        SUCCESS,
        FAILURE,
    }

    [Serializable]
    public class SeasonRewardRecord
    {
        public string agentAddress;
        public string avatarAddress;
        public int raidId;
        public int ranking;
        public SeasonRewards[] rewards;
    }

    [Serializable]
    public class SeasonRewards
    {
        public int amount;
        public string ticker;
        public string tx_id;
        public string tx_result;
    }
}
