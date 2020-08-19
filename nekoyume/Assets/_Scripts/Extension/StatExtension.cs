using Nekoyume.Model.Stat;


namespace Nekoyume
{
    public static class StatExtension
    {
        public static string DecimalStatToString(this DecimalStat stat)
        {
            var value = stat.Type == StatType.SPD ?
                (stat.Value / 100m) : stat.Value;

            return $"{stat.Type} +{(float)value}";
        }
    }
}
