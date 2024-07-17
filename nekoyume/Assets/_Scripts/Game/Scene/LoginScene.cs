#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif
#if !UNITY_EDITOR && UNITY_STANDALONE
#define RUN_ON_STANDALONE
#endif

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using JetBrains.Annotations;
using Lib9c.Formatters;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using LruCacheNet;
using MessagePack;
using MessagePack.Resolvers;
using Nekoyume.Action;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Extensions;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Multiplanetary;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Game.OAuth;
using Nekoyume.Game.VFX;
using Nekoyume.Helper;
using Nekoyume.IAPStore;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.Pattern;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;
using NineChronicles.ExternalServices.IAPService.Runtime;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using UnityEngine;
using UnityEngine.Playables;
using Currency = Libplanet.Types.Assets.Currency;
using Menu = Nekoyume.UI.Menu;
using Random = UnityEngine.Random;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using Nekoyume.Model.Mail;
using Debug = UnityEngine.Debug;
#if ENABLE_FIREBASE
using NineChronicles.GoogleServices.Firebase.Runtime;
#endif

namespace Nekoyume.Game.Scene
{
    public class LoginScene : MonoBehaviour
    {
        [SerializeField] private IntroScreen introScreen;
        [SerializeField] private Synopsis synopsis;

        private IEnumerator Start()
        {
            var game = Game.instance;
            var commandLineOptions = game.CommandLineOptions;
            yield return ResourceManager.Instance.InitializeAsync().ToCoroutine();

#if LIB9C_DEV_EXTENSIONS && UNITY_ANDROID
            Lib9c.DevExtensions.TestbedHelper.LoadTestbedCreateAvatarForQA();
#endif
            NcDebug.Log($"[{nameof(LoginScene)}] Start() invoked");
            var totalSw = new Stopwatch();
            totalSw.Start();
            
            game.AddRequestManager();
            yield return game.InitializeLiveAssetManager();

            // NOTE: Initialize KeyManager after load CommandLineOptions.
            if (!KeyManager.Instance.IsInitialized)
            {
                KeyManager.Instance.Initialize(
                    commandLineOptions.KeyStorePath,
                    Helper.Util.AesEncrypt,
                    Helper.Util.AesDecrypt);
            }

            // NOTE: Try to sign in with the first registered key
            //       if the CommandLineOptions.PrivateKey is empty in mobile.
            if (Platform.IsMobilePlatform() &&
                string.IsNullOrEmpty(commandLineOptions.PrivateKey) &&
                KeyManager.Instance.TrySigninWithTheFirstRegisteredKey())
            {
                NcDebug.Log("[LoginScene] Start()... CommandLineOptions.PrivateKey is empty in mobile." +
                    " Set cached private key instead.");
                commandLineOptions.PrivateKey =
                    KeyManager.Instance.SignedInPrivateKey.ToHexWithZeroPaddings();
            }
            
            yield return game.InitializeL10N();
            

            // NOTE: Initialize planet registry.
            //       It should do after load CommandLineOptions.
            //       And it should do before initialize Agent.
            var planetContext = new PlanetContext(commandLineOptions);
            yield return PlanetSelector.InitializePlanetRegistryAsync(planetContext).ToCoroutine();

#if RUN_ON_MOBILE
            if (planetContext.HasError)
            {
                Game.QuitWithMessage(
                    L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                    planetContext.Error);
                yield break;
            }
#else
            NcDebug.Log("[LoginScene] UpdateCurrentPlanetIdAsync()... Try to set current planet id.");
            if (planetContext.IsSkipped)
            {
                NcDebug.LogWarning("[LoginScene] UpdateCurrentPlanetIdAsync()... planetContext.IsSkipped is true." +
                    "\nYou can consider to use CommandLineOptions.SelectedPlanetId instead.");
            }
            else if (planetContext.HasError)
            {
                NcDebug.LogWarning("[LoginScene] UpdateCurrentPlanetIdAsync()... planetContext.HasError is true." +
                    "\nYou can consider to use CommandLineOptions.SelectedPlanetId instead.");
            }
            else if (planetContext.PlanetRegistry!.TryGetPlanetInfoByHeadlessGrpc(commandLineOptions.RpcServerHost, out var planetInfo))
            {
                NcDebug.Log("[LoginScene] UpdateCurrentPlanetIdAsync()... planet id is found in planet registry.");
                game.CurrentPlanetId = planetInfo.ID;
            }
            else if (!string.IsNullOrEmpty(commandLineOptions.SelectedPlanetId))
            {
                NcDebug.Log("[LoginScene] UpdateCurrentPlanetIdAsync()... SelectedPlanetId is not null.");
                game.CurrentPlanetId = new PlanetId(commandLineOptions.SelectedPlanetId);
            }
            else
            {
                NcDebug.LogWarning("[LoginScene] UpdateCurrentPlanetIdAsync()... planet id is not found in planet registry." +
                    "\nCheck CommandLineOptions.PlaneRegistryUrl and CommandLineOptions.RpcServerHost.");
            }

            if (game.CurrentPlanetId is not null)
            {
                planetContext = PlanetSelector.SelectPlanetById(planetContext, game.CurrentPlanetId.Value);
            }
#endif
            // ~Initialize planet registry

            game.SetPortalConnect();
            
#if ENABLE_FIREBASE
            // NOTE: Initialize Firebase.
            yield return FirebaseManager.InitializeAsync().ToCoroutine();
#endif

            // TODO: 내부 분기 제거, 인스턴스 관리 주체 Analyzer내부에서 하도록 변경
            // NOTE: Initialize Analyzer after load CommandLineOptions, initialize States,
            //       initialize Firebase Manager.
            //       The planetId is null because it is not initialized yet. It will be
            //       updated after initialize Agent.
            game.InitializeAnalyzer(
                commandLineOptions.PrivateKey is null
                    ? null
                    : PrivateKey.FromString(commandLineOptions.PrivateKey).Address,
                null,
                commandLineOptions.RpcServerHost);
            game.Analyzer.Track("Unity/Started");
            
            game.InitializeMessagePackResolver();
            
            if (game.CheckRequiredUpdate())
            {
                // NOTE: Required update is detected.
                yield break;
            }
            
            // NOTE: Apply l10n to IntroScreen after L10nManager initialized.
            game.InitializeFirstResources();
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);

            // NOTE: Initialize IAgent.
            var agentInitialized = false;
            var agentInitializeSucceed = false;
            yield return StartCoroutine(game.CoLogin(planetContext, succeed =>
                    {
                        NcDebug.Log($"[LoginScene] Agent initialized. {succeed}");
                        agentInitialized = true;
                        agentInitializeSucceed = succeed;
                    }
                )
            );

            var grayLoadingScreen = Widget.Find<GrayLoadingScreen>();
            grayLoadingScreen.ShowProgress(GameInitProgress.ProgressStart);
            yield return new WaitUntil(() => agentInitialized);
#if RUN_ON_MOBILE
            if (planetContext?.HasError ?? false)
            {
                Game.QuitWithMessage(
                    L10nManager.Localize("ERROR_INITIALIZE_FAILED"),
                    planetContext.Error);
                yield break;
            }

            if (planetContext.SelectedPlanetInfo is null)
            {
                Game.QuitWithMessage("planetContext.CurrentPlanetInfo is null in mobile.");
                yield break;
            }

            game.CurrentPlanetId = planetContext.SelectedPlanetInfo.ID;
#endif

            Analyzer.SetPlanetId(game.CurrentPlanetId?.ToString());
            NcDebug.Log($"[LoginScene] Start()... CurrentPlanetId updated. {game.CurrentPlanetId?.ToString()}");

            // TODO: 위 Login코드에서 처리하면되는거아닌가? 생각이 들지만 기존 코드 유지.. 이부분 추후 확인
            if (agentInitializeSucceed)
            {
                game.OnAgentInitializeSucceed();
            }
            else
            {
                game.OnAgentInitializeFailed();
                yield break;
            }

            game.SetActionManager();
            ApiClients.Instance.Initialize(commandLineOptions);

            StartCoroutine(game.InitializeIAP());

            yield return StartCoroutine(game.InitializeWithAgent());

            yield return CharacterManager.Instance.LoadCharacterAssetAsync().ToCoroutine();
            var createSecondWidgetCoroutine = StartCoroutine(MainCanvas.instance.CreateSecondWidgets());
            yield return createSecondWidgetCoroutine;

            var initializeSecondWidgetsCoroutine = StartCoroutine(game.CoInitializeSecondWidget());

#if RUN_ON_MOBILE
            // Note : Social Login 과정을 거친 경우만 토큰을 확인합니다.
            if (!game.IsGuestLogin && !SigninContext.HasSignedWithKeyImport)
            {
                var checkTokensTask = game.PortalConnect.CheckTokensAsync(game.States.AgentState.address);
                yield return checkTokensTask.AsCoroutine();
                if (!checkTokensTask.Result)
                {
                    Game.QuitWithMessage(L10nManager.Localize("ERROR_INITIALIZE_FAILED"), "Failed to Get Tokens.");
                    yield break;
                }

                if (!planetContext.IsSelectedPlanetAccountPledged)
                {
                    yield return StartCoroutine(game.CoCheckPledge(planetContext.SelectedPlanetInfo.ID));
                }
            }
#endif

#if UNITY_EDITOR_WIN
            game.UseMarketService();
#endif
            // Initialize D:CC NFT data
            StartCoroutine(game.CoInitDccAvatar());
            StartCoroutine(game.CoInitDccConnecting());
            yield return initializeSecondWidgetsCoroutine;
            grayLoadingScreen.ShowProgress(GameInitProgress.ProgressCompleted);
            Analyzer.Instance.Track("Unity/Intro/Start/SecondWidgetCompleted");

            var secondWidgetCompletedEvt = new AirbridgeEvent("Intro_Start_SecondWidgetCompleted");
            AirbridgeUnity.TrackEvent(secondWidgetCompletedEvt);

            game.InitializeStage();

            // Initialize Rank.SharedModel
            RankPopup.UpdateSharedModel();
            Helper.Util.TryGetAppProtocolVersionFromToken(
                commandLineOptions.AppProtocolVersion,
                out var appProtocolVersion);
            Widget.Find<VersionSystem>().SetVersion(appProtocolVersion);
            Analyzer.Instance.Track("Unity/Intro/Start/ShowNext");

            var showNextEvt = new AirbridgeEvent("Intro_Start_ShowNext");
            AirbridgeUnity.TrackEvent(showNextEvt);

            StartCoroutine(game.CoUpdate());
            game.ReservePushNotifications();

            yield return new WaitForSeconds(GrayLoadingScreen.SliderAnimationDuration);
            game.IsInitialized = true;
            Widget.Find<IntroScreen>().Close();
            EnterNext().Forget();
            totalSw.Stop();
            NcDebug.Log($"[LoginScene] Game Start End. {totalSw.ElapsedMilliseconds}ms.");
        }

        private async UniTask EnterNext()
        {
            NcDebug.Log("[LoginScene] EnterNext() invoked");
            if (!GameConfig.IsEditor)
            {
                if (States.Instance.AgentState.avatarAddresses.Any() &&
                    Helper.Util.TryGetStoredAvatarSlotIndex(out var slotIndex) &&
                    States.Instance.AvatarStates.ContainsKey(slotIndex))
                {
                    var loadingScreen = Widget.Find<LoadingScreen>();
                    loadingScreen.Show(
                        LoadingScreen.LoadingType.Entering,
                        L10nManager.Localize("UI_LOADING_BOOTSTRAP_START"));
                    var sw = new Stopwatch();
                    sw.Reset();
                    sw.Start();
                    await RxProps.SelectAvatarAsync(slotIndex, Game.instance.Agent.BlockTipStateRootHash, true);
                    sw.Stop();
                    NcDebug.Log("[LoginScene] EnterNext()... SelectAvatarAsync() finished in" +
                        $" {sw.ElapsedMilliseconds}ms.(elapsed)");
                    loadingScreen.Close();
                    Event.OnRoomEnter.Invoke(false);
                    Event.OnUpdateAddresses.Invoke();
                }
                else
                {
                    Widget.Find<Synopsis>().Show();
                }
            }
            else
            {
                PlayerFactory.Create();

                if (Helper.Util.TryGetStoredAvatarSlotIndex(out var slotIndex) &&
                    States.Instance.AvatarStates.ContainsKey(slotIndex))
                {
                    var avatarState = States.Instance.AvatarStates[slotIndex];
                    if (avatarState?.inventory == null ||
                        avatarState.questList == null ||
                        avatarState.worldInformation == null)
                    {
                        EnterLogin();
                    }
                    else
                    {
                        var sw = new Stopwatch();
                        sw.Reset();
                        sw.Start();
                        await RxProps.SelectAvatarAsync(slotIndex, Game.instance.Agent.BlockTipStateRootHash, true);
                        sw.Stop();
                        NcDebug.Log("[LoginScene] EnterNext()... SelectAvatarAsync() finished in" +
                            $" {sw.ElapsedMilliseconds}ms.(elapsed)");
                        Event.OnRoomEnter.Invoke(false);
                        Event.OnUpdateAddresses.Invoke();
                    }
                }
                else
                {
                    EnterLogin();
                }
            }

            Widget.Find<GrayLoadingScreen>().Close();
        }

        private static void EnterLogin()
        {
            NcDebug.Log("[LoginScene] EnterLogin() invoked");
            Widget.Find<Login>().Show();
            Event.OnNestEnter.Invoke();
        }
    }
}
