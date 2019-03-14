using System.Collections.Generic;

namespace Nekoyume.Data.Table
{
    public class Recipe : Row
    {
        public int Id;
        public int Material1;
        public int Material2;
        public int Material3;

        public List<int> Materials => new List<int>
        {
            Material1,
            Material2,
            Material3
        };
    }
}
