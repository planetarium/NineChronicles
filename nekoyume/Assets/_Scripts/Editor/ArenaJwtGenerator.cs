using UnityEngine;
using UnityEditor;
using Libplanet.Crypto;
using Nekoyume.ApiClient;
using Libplanet.Common;
using Nekoyume.State;
using Nekoyume.Blockchain;
using Nekoyume.UI;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using Nekoyume.Helper;

public class ArenaJwtGenerator : EditorWindow
{
    private string loginPassword = "";
    private string generatedJwt = "";
    private int selectedAccountIndex = 0;
    private List<Address> registeredAccounts;
    private string[] accountAddresses;

    [MenuItem("Tools/Arena/JWT Generator")]
    public static void ShowWindow()
    {
        GetWindow<ArenaJwtGenerator>("Arena JWT Generator");
    }

    private void OnEnable()
    {
        RefreshAccountList();
    }

    private void RefreshAccountList()
    {
        Debug.Log("Trying to reset password...");
        if (!KeyManager.Instance.IsInitialized)
        {
            KeyManager.Instance.Initialize(
                null,
                Util.AesEncrypt,
                Util.AesDecrypt);
        }
        
        if (KeyManager.Instance == null)
        {
            Debug.LogError("KeyManager.Instance is null");
            accountAddresses = new[] { "KeyManager not initialized" };
            registeredAccounts = new List<Address>();
            return;
        }

        var keyList = KeyManager.Instance.GetList();
        if (keyList == null || !keyList.Any())
        {
            accountAddresses = new[] { "No registered accounts" };
            registeredAccounts = new List<Address>();
            return;
        }

        registeredAccounts = keyList.Where(tuple => tuple.Item2 != null)
            .Select(tuple => tuple.Item2.Address)
            .ToList();

        accountAddresses = registeredAccounts
            .Select(address => address.ToString())
            .ToArray();

        if (accountAddresses.Length == 0)
        {
            accountAddresses = new[] { "No registered accounts" };
        }

        // 선택된 인덱스가 범위를 벗어나지 않도록 보정
        selectedAccountIndex = Mathf.Clamp(selectedAccountIndex, 0, accountAddresses.Length - 1);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Arena JWT Generator", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // 계정 목록 새로고침 버튼
        if (GUILayout.Button("Refresh Account List"))
        {
            RefreshAccountList();
        }

        EditorGUILayout.Space();

        // 등록된 계정 선택 드롭다운
        EditorGUI.BeginDisabledGroup(registeredAccounts == null || registeredAccounts.Count == 0);

        EditorGUILayout.LabelField("Select Account:", EditorStyles.boldLabel);
        selectedAccountIndex = EditorGUILayout.Popup(selectedAccountIndex, accountAddresses);

        if (registeredAccounts != null && registeredAccounts.Count > 0)
        {
            EditorGUILayout.LabelField("Selected Address:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(accountAddresses[selectedAccountIndex],
                EditorStyles.textField, GUILayout.Height(20));
        }

        EditorGUILayout.Space();

        // 패스워드 입력 필드
        loginPassword = EditorGUILayout.PasswordField("Password", loginPassword);

        EditorGUILayout.Space();

        if (GUILayout.Button("Login & Generate JWT"))
        {
            try
            {
                if (KeyManager.Instance == null)
                {
                    EditorUtility.DisplayDialog("Error", "KeyManager is not initialized", "OK");
                    return;
                }

                if (registeredAccounts == null || registeredAccounts.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error", "No registered accounts found", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(loginPassword))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter password", "OK");
                    return;
                }

                var selectedAddress = registeredAccounts[selectedAccountIndex];
                if (selectedAddress == null)
                {
                    EditorUtility.DisplayDialog("Error", "Selected address is invalid", "OK");
                    return;
                }

                var keyList = KeyManager.Instance.GetList();
                if (keyList == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to get key list", "OK");
                    return;
                }

                var selectedKey = keyList.FirstOrDefault(x =>
                    x.Item2 != null && x.Item2.Address.Equals(selectedAddress));
                if (selectedKey.Item2 == null)
                {
                    EditorUtility.DisplayDialog("Error", "Selected key not found", "OK");
                    return;
                }

                try
                {
                    // ProtectedPrivateKey의 Unprotect를 사용하여 privateKey 생성
                    var privateKey = selectedKey.Item2.Unprotect(loginPassword);
                    if (privateKey == null)
                    {
                        throw new Exception("Failed to decrypt private key");
                    }

                    // 생성된 privateKey로 JWT 생성
                    generatedJwt = ArenaServiceManager.CreateJwt(
                        privateKey,
                        selectedAddress.ToString()
                    );

                    Debug.Log($"Login successful. Address: {selectedAddress}");
                    Debug.Log($"Generated JWT: {generatedJwt}");

                    // JWT를 클립보드에 자동 복사
                    EditorGUIUtility.systemCopyBuffer = generatedJwt;
                    EditorUtility.DisplayDialog("Success",
                        $"Login successful with address:\n{selectedAddress}\n\nJWT has been generated and copied to clipboard",
                        "OK");
                }
                catch
                {
                    EditorUtility.DisplayDialog("Error", "Login failed. Please check your password.", "OK");
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"JWT 생성 실패: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate JWT: {e.Message}", "OK");
            }
        }

        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(generatedJwt))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated JWT:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(generatedJwt);

            if (GUILayout.Button("Copy to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = generatedJwt;
            }
        }
    }
}