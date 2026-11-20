namespace Nekoyume.Helper
{
    /// <summary>
    /// 등급별 화면 표시 규칙. 9등급부터는 1~8등급 색을 순환해 다시 쓰고((등급-1)%8+1), 금테로 구분한다
    /// (<see cref="SpriteHelper"/> 안쪽 배경, <c>LocalizationExtensions.GetItemGradeColor</c> 글자색과 같은 규칙).
    /// 등급별 ScriptableObject 에 새 등급 항목이 아직 없을 때 이 색 등급의 항목으로 대신한다.
    /// </summary>
    public static class GradeVisualHelper
    {
        public const int ColorCycle = 8;

        /// <summary>그 등급이 쓰는 1~8 색 등급. 0 이하(초기 지급 장비)·1~8 은 그대로.</summary>
        public static int ColorGrade(int grade)
        {
            return grade > ColorCycle ? (grade - 1) % ColorCycle + 1 : grade;
        }
    }
}
