using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    // todo: 카테고리 버튼의 성격 상 `IToggleable`을 구현하고, 사용하는 쪽에서 `ToggleGroup` 객체를 통해서 상태를 관리하는 것이 좋겠음.
    public class CategoryButton : NormalButton
    {
        public Sprite toggledOn;
        public Sprite toggledOff;
        
        protected override void Awake()
        {
            base.Awake();
            
            button.OnClickAsObservable().Subscribe(_ =>
            {
                
            }).AddTo(gameObject);
        }
        
        public void SetToggledOn()
        {
            image.sprite = toggledOn;
        }
        public void SetToggledOff()
        {
            image.sprite = toggledOff;
        }
    }
}
