using Nekoyume.Model.Stat;


namespace Nekoyume
{
    public static class StatExtensions
    {
        public static string DecimalStatToString(this DecimalStat stat)
        {
            var value = stat.Type == StatType.SPD ?
                (stat.Value / 100m) : stat.Value;

            return $"{stat.Type} +{(float)value}";
        }
    }
}
