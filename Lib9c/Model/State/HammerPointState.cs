using System;
using Bencodex.Types;
using Libplanet;
using Nekoyume.TableData.Crystal;

namespace Nekoyume.Model.State
{
    public class HammerPointState : IState
    {
        public Address Address { get; }
        public int RecipeId { get; }
        public int HammerPoint { get; private set; }

        public HammerPointState(Address address, int recipeId)
        {
            Address = address;
            RecipeId = recipeId;
            HammerPoint = 0;
        }

        public HammerPointState(Address address, List serialized)
        {
            Address = address;
            RecipeId = serialized[0].ToInteger();
            HammerPoint = serialized[1].ToInteger();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(RecipeId.Serialize())
                .Add(HammerPoint.Serialize());
        }

        public void AddHammerPoint(int point, CrystalHammerPointSheet sheet = null)
        {
            if (sheet == null)
            {
                HammerPoint += point;
                return;
            }

            if (sheet.TryGetValue(RecipeId, out var row))
            {
                HammerPoint = Math.Min(HammerPoint + point, row.MaxPoint);
            }
        }

        public void ResetHammerPoint()
        {
            HammerPoint = 0;
        }
    }
}
