using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet;

namespace Nekoyume.TableData.GrandFinale
{
    using static TableExtensions;

    [Serializable]
    public class GrandFinaleParticipantsSheet : Sheet<int, GrandFinaleParticipantsSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => GrandFinaleId;

            public int GrandFinaleId { get; private set; }

            public List<Address> Participants { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                GrandFinaleId = ParseInt(fields[0]);
                Participants = new List<Address> {new Address(fields[1])};
            }
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.Participants.Any())
            {
                return;
            }

            row.Participants.Add(value.Participants[0]);
        }

        public GrandFinaleParticipantsSheet() : base(nameof(GrandFinaleParticipantsSheet))
        {
        }
    }
}
