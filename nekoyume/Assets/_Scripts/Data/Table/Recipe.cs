using System.Collections.Generic;

namespace Nekoyume.Data.Table
{
    public class Recipe : Row
    {
        public int Id;
        public int Material_1;
        public int Material_2;
        public int Material_3;

        public List<int> Materials => new List<int>
        {
            Material_1,
            Material_2,
            Material_3
        };
    }
}
