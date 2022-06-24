using System;
using Bencodex.Types;
using Nekoyume.Model.State;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.Arena
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1027
    /// </summary>
    public class ArenaScore : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int championshipId, int round) =>
            avatarAddress.Derive($"arena_score_{championshipId}_{round}");

        public const int ArenaScoreDefault = 1000;

        public Address Address;
        public int Score { get; private set; }

        public ArenaScore(Address avatarAddress, int championshipId, int round, int score = ArenaScoreDefault)
        {
            Address = DeriveAddress(avatarAddress, championshipId, round);
            Score = score;
        }

        public ArenaScore(List serialized)
        {
            Address = serialized[0].ToAddress();
            Score = (Integer)serialized[1];
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(Score);
        }

        public void AddScore(int score)
        {
            Score = Math.Max(Score + score, ArenaScoreDefault);
        }
    }
}
