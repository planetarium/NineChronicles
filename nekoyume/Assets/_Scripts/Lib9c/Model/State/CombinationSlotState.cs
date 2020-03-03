using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.State
{
    public class CombinationSlotState : State
    {
        public const string DeriveFormat = "combination-slot-{0}";
        public long UnlockBlockIndex { get; private set; }
        public int UnlockStage { get; private set; }
        public Combination.ResultModel Result { get; private set; }

        public CombinationSlotState(Address address, int unlockStage) : base(address)
        {
            UnlockStage = unlockStage;
        }

        public CombinationSlotState(Dictionary serialized) : base(serialized)
        {
            UnlockBlockIndex = serialized["unlockBlockIndex"].ToLong();
            UnlockStage = serialized["unlockStage"].ToInteger();
            if (serialized.TryGetValue((Text) "result", out var result))
            {
                Result = new Combination.ResultModel((Dictionary)result);
            }
        }

        public bool Validate(AvatarState avatarState, long blockIndex)
        {
            return avatarState.worldInformation.IsClearedStage(UnlockStage)
                   && blockIndex >= UnlockBlockIndex;
        }

        public void Update(Combination.ResultModel result, long recipeRequiredBlockIndex)
        {
            Result = result;
            UnlockBlockIndex = recipeRequiredBlockIndex;
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text) "unlockBlockIndex"] = UnlockBlockIndex.Serialize(),
                [(Text) "unlockStage"] = UnlockStage.Serialize(),
            };
            if (!(Result is null))
            {
                values.Add((Text) "result", Result.Serialize());
            }
            return new Dictionary(values.Union((Dictionary) base.Serialize()));
        }
    }
}
