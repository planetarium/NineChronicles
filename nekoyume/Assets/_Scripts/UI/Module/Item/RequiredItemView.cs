using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module
{
    public class RequiredItemView : SimpleCountableItemView
    {
        protected const string CountTextFormatEnough = "{0}/{1}";
        protected const string CountTextFormatNotEnough = "<color=red>{0}</color>/{1}";

        public int RequiredCount { get; set; } = 1;

        public void SetData(CountableItem model, int requiredCount)
        {
            RequiredCount = requiredCount;
            base.SetData(model);
        }

        protected override void SetCount(int count)
        {
            countText.text = string.Format(count >= RequiredCount ?
                CountTextFormatEnough :
                CountTextFormatNotEnough,
                Model.Count.Value, RequiredCount);
        }
    }
}
