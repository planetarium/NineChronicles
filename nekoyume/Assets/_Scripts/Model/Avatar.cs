using System.Collections.Generic;
using System.Linq;
using Nekoyume.Move;

namespace Nekoyume.Model
{
    [System.Serializable]
    public class Avatar
    {
        public string Name;
        public int Level;
        public long EXP;
        public int HPMax;
        public int CurrentHP;
        public string Items;
        public int WorldStage;
        public bool Dead = false;

        public static Avatar FromMoves(IEnumerable<MoveBase> moves)
        {
            var createNovice = moves.FirstOrDefault() as CreateNovice;
            if (createNovice == null)
            {
                return null;
            }

            var ctx = new Context();
            var avatar = createNovice.Execute(ctx).Avatar;

            foreach (var move in moves.Skip(1))
            {
                avatar = move.Execute(ctx).Avatar;
            }

            return avatar;
        }
    }
}
