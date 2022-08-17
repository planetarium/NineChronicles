using System;

namespace Nekoyume.TableData.Event
{
    [Serializable]
    public class EventConsumableItemRecipeSheet
        : Sheet<int, EventConsumableItemRecipeSheet.Row>
    {
        [Serializable]
        public class Row : ConsumableItemRecipeSheet.Row
        {
        }

        public EventConsumableItemRecipeSheet()
            : base(nameof(EventConsumableItemRecipeSheet))
        {
        }
    }
}
