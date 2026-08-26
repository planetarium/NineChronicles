using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nekoyume
{
    /// <summary>
    /// batchmode 에서 OpenAPI 클라이언트를 재생성하기 위한 진입점.
    ///
    /// OpenApiGenerator 는 EditorWindow 라 사람이 창을 띄워야 하고, RefreshAll 은 기존 파일
    /// 헤더의 "Source URL" 을 그대로 재사용한다 — IAP 생성물의 그 URL 은 이미 죽은 Lambda
    /// (xd2n1dpmce.execute-api…) 라서 새로고침이 불가능하다. 그래서 URL 을 인자로 받아
    /// GenerateOpenApiClass 를 직접 부른다(헤더의 Source URL 도 이 값으로 다시 써진다).
    ///
    /// 사용:
    ///   Unity -batchmode -quit -projectPath nekoyume \
    ///         -executeMethod Nekoyume.OpenApiBatchGenerate.Run \
    ///         -openApiUrl https://… -openApiClass InAppPurchaseServiceClient
    /// </summary>
    public static class OpenApiBatchGenerate
    {
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            var url = ArgValue(args, "-openApiUrl");
            var className = ArgValue(args, "-openApiClass");

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(className))
            {
                Debug.LogError("[OpenApiBatchGenerate] -openApiUrl / -openApiClass 필요");
                EditorApplication.Exit(2);
                return;
            }

            Debug.Log($"[OpenApiBatchGenerate] {className} <- {url}");
            OpenApiGenerator.GenerateOpenApiClass(url, className);
            AssetDatabase.Refresh();
            Debug.Log("[OpenApiBatchGenerate] done");
        }

        private static string ArgValue(string[] args, string key)
        {
            var i = Array.IndexOf(args, key);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
