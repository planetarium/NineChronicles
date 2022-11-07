using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class MaterialCraftMail : Mail
    {
        public int ItemCount;
        public Material Material;

        public MaterialCraftMail(
            long blockIndex,
            Guid id,
            long requiredBlockIndex,
            int itemCount,
            Material material
        ) : base(blockIndex, id, requiredBlockIndex)
        {
            ItemCount = itemCount;
            Material = material;
        }

        public MaterialCraftMail(Dictionary serialized) : base(serialized)
        {
            ItemCount = serialized["ic"].ToInteger();
            Material = (Material)ItemFactory.Deserialize((Dictionary)serialized["m"]);
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Workshop;

        protected override string TypeId => nameof(MaterialCraftMail);
        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"ic"] = ItemCount.Serialize(),
                [(Text)"m"] = Material.Serialize(),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002

    }
}
