using UnityEngine;
using OneStore.Common.Internal;
using System;

// NOTE(9c): 플러그인 v1.3.4 버그 수정. JniHelper 는 `#if UNITY_ANDROID || !UNITY_EDITOR` 로 가드돼
// 있는데 이 파일에는 가드가 없어서, 빌드 타깃이 Android 가 아니면 JniHelper 가 컴파일 대상에서
// 빠진 채 이 파일만 남아 CS0103 이 난다. JniHelper 를 참조하는 나머지 네 파일은 모두 이 가드를
// 갖고 있으므로 동일하게 맞춘다. 플러그인을 다시 임포트하면 이 수정이 사라진다.
#if UNITY_ANDROID || !UNITY_EDITOR
namespace OneStore.Common
{
    /// <summary>
    /// The StoreEnvironment class is responsible for determining the type of store where the app is installed.
    /// </summary>
    public class StoreEnvironment
    {
        /// <summary>
        /// Represents the StoreEnvironment Java class.
        /// </summary>
        private static readonly AndroidJavaClass storeEnvironmentClass = new AndroidJavaClass(Constants.StoreEnvironment);

        /// <summary>
        /// Determines the store type where the app was installed.<br/>
        /// <br/>
        /// @return One of the following values: <br/>
        ///         - <see cref="StoreType.ONESTORE"/>: Installed from ONE Store or a trusted store. <br/>
        ///         - <see cref="StoreType.VENDING"/>: Installed from Google Play Store. <br/>
        ///         - <see cref="StoreType.ETC"/>: Installed from other stores. <br/>
        ///         - <see cref="StoreType.UNKNOWN"/>: Store information is unknown. <br/>
        /// </summary>
        /// <returns>A <see cref="StoreType"/> value representing the app's installation source.</returns>
        public static StoreType GetStoreType()
        {
            var storeTypeValue = storeEnvironmentClass.CallStatic<int>(
                Constants.StoreEnvironmentGetStoreTypeMethod,
                JniHelper.GetApplicationContext()
            );
            return Enum.IsDefined(typeof(StoreType), storeTypeValue) ? (StoreType)storeTypeValue : StoreType.UNKNOWN;
        }
    }
}
#endif
