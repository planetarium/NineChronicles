using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NekoyumeEditor
{
    /// <summary>
    /// 원스토어 빌드를 가르는 <c>ONESTORE</c> 스크립팅 디파인 심볼을 켜고 끈다.
    /// </summary>
    /// <remarks>
    /// **기본값은 꺼짐이다.** 켜지 않은 Android 빌드는 지금까지처럼 Unity IAP → Google Play 로 간다.
    /// 켠 빌드만 원스토어 결제 경로를 탄다.
    ///
    /// 심볼은 프로젝트 설정에 저장되므로 <c>ProjectSettings.asset</c> 이 바뀐다. 켠 채로 커밋하면
    /// 팀 전체 Android 빌드가 원스토어가 되니, 로컬에서 확인한 뒤에는 다시 꺼야 한다.
    ///
    /// CI 는 이 클래스를 쓰지 않는다. 심볼은 **컴파일 시점**에 정해지는데 <c>-executeMethod</c> 는
    /// 컴파일이 끝난 뒤에 돌아서 <c>#if ONESTORE</c> 가 먹지 않기 때문이다. 그래서
    /// <c>android-build-and-release.yml</c> 이 빌드 전에 <c>ProjectSettings.asset</c> 을 직접 고친다
    /// (<c>onestore</c> 입력). <see cref="EnableFromCommandLine"/> 은 별도로 Unity 를 한 번 더 띄울 수
    /// 있는 경우를 위해 남겨 둔다.
    /// </remarks>
    public static class OneStoreDefine
    {
        private const string Symbol = "ONESTORE";
        private const string MenuEnable = "Build/OneStore/Enable ONESTORE define";
        private const string MenuDisable = "Build/OneStore/Disable ONESTORE define";

        private static readonly NamedBuildTarget Target = NamedBuildTarget.Android;

        [MenuItem(MenuEnable)]
        public static void Enable() => Set(true);

        [MenuItem(MenuEnable, isValidateFunction: true)]
        private static bool ValidateEnable()
        {
            Menu.SetChecked(MenuEnable, IsEnabled());
            return !IsEnabled();
        }

        [MenuItem(MenuDisable)]
        public static void Disable() => Set(false);

        [MenuItem(MenuDisable, isValidateFunction: true)]
        private static bool ValidateDisable()
        {
            Menu.SetChecked(MenuDisable, !IsEnabled());
            return IsEnabled();
        }

        /// <summary>
        /// 배치 모드 진입점. <c>-executeMethod NekoyumeEditor.OneStoreDefine.EnableFromCommandLine</c>
        /// 로 부르되, **빌드와 같은 Unity 실행에서 부르면 안 된다**(위 remarks 참고).
        /// </summary>
        public static void EnableFromCommandLine() => Set(true);

        public static bool IsEnabled() => Current().Contains(Symbol);

        private static string[] Current()
        {
            return PlayerSettings.GetScriptingDefineSymbols(Target)
                .Split(';')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }

        private static void Set(bool enabled)
        {
            var symbols = Current().ToList();
            if (enabled)
            {
                if (symbols.Contains(Symbol))
                {
                    return;
                }

                symbols.Add(Symbol);
            }
            else if (!symbols.Remove(Symbol))
            {
                return;
            }

            PlayerSettings.SetScriptingDefineSymbols(Target, string.Join(";", symbols));
            Debug.Log($"[OneStoreDefine] Android ONESTORE = {enabled}. symbols: {string.Join(";", symbols)}");
        }
    }
}
