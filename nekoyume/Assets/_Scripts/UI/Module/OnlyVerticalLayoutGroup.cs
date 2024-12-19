using UnityEngine;
using UnityEngine.UI;

public class OnlyVerticalLayoutGroup : VerticalLayoutGroup
{
    public override void SetLayoutHorizontal()
    {
        // base.SetLayoutHorizontal(); 호출하지 않음
        // 혹은 내부에서 x좌표 정렬 로직 제거
    }

    public override void SetLayoutVertical()
    {
        base.SetLayoutVertical();
    }
}
