using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.State
{
    public class CombinationSlotState : State
    {
        public const string DeriveFormat = "combination-slot-{0}";
        public long UnlockBlockIndex { get; private set; }
        public int UnlockStage { get; private set; }
        public long StartBlockIndex { get; private set; }
        public AttachmentActionResult Result { get; private set; }
        public long RequiredBlockIndex => UnlockBlockIndex - StartBlockIndex;

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
                Result = AttachmentActionResult.Deserialize((Dictionary) result);
            }

            if (serialized.TryGetValue((Text) "startBlockIndex", out var value))
            {
                StartBlockIndex = value.ToLong();
            }
        }

        public bool Validate(AvatarState avatarState, long blockIndex)
        {
            return avatarState.worldInformation.IsStageCleared(UnlockStage)
                   && blockIndex >= UnlockBlockIndex;
        }

        public void Update(AttachmentActionResult result, long blockIndex, long requiredBlockIndex)
        {
            Result = result;
            StartBlockIndex = blockIndex;
            UnlockBlockIndex = requiredBlockIndex;
        }

        public void Update(long blockIndex)
        {
            UnlockBlockIndex = blockIndex;
            Result.itemUsable.Update(blockIndex);
        }

        public void Update(long blockIndex, Material material, int count)
        {
            Update(blockIndex);
            var result = new RapidCombination.ResultModel((Dictionary) Result.Serialize())
            {
                cost = new Dictionary<Material, int> {[material] = count}
            };
            Result = result;
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text) "unlockBlockIndex"] = UnlockBlockIndex.Serialize(),
                [(Text) "unlockStage"] = UnlockStage.Serialize(),
                [(Text) "startBlockIndex"] = StartBlockIndex.Serialize(),
            };
            if (!(Result is null))
            {
                values.Add((Text) "result", Result.Serialize());
            }
            return new Dictionary(values.Union((Dictionary) base.Serialize()));
        }
    }
}
