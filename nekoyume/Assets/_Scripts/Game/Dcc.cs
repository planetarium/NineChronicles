using System.Collections.Generic;

namespace Nekoyume.Game
{
    public class Dcc
    {
        public Dictionary<string, long> Avatars { get; }

        public Dcc(Dictionary<string, long> avatars)
        {
            Avatars = avatars;
        }
    }
}
