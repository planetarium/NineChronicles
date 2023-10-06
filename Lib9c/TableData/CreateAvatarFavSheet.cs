using System;
using System.Collections.Generic;
using Lib9c;
using Libplanet.Types.Assets;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class CreateAvatarFavSheet: Sheet<string, CreateAvatarFavSheet.Row>
    {
        public class Row : SheetRow<string>
        {
            public override string Key => Currency.Ticker;
            public Currency Currency { get; private set; }
            public int Quantity { get; private set; }
            public Target Target { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Currency = Currencies.GetMinterlessCurrency(fields[0]);
                Quantity = ParseInt(fields[1]);
                Target = (Target) Enum.Parse(typeof(Target), fields[2]);
            }
        }

        public enum Target
        {
            Agent,
            Avatar,
        }

        public CreateAvatarFavSheet() : base(nameof(CreateAvatarFavSheet))
        {
        }
    }
}
