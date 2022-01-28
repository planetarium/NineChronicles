using System;
using UniRx;

namespace Nekoyume.L10n
{
    [Serializable]
    public class LanguageTypeReactiveProperty : ReactiveProperty<LanguageType>
    {
        public LanguageTypeReactiveProperty() : base()
        {
        }
        
        public LanguageTypeReactiveProperty(LanguageType initialValue) : base(initialValue)
        {
        }
    }
}
