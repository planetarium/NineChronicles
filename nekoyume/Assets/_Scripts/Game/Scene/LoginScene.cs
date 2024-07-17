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
    using GraphQL;
    using Arena;
    using Nekoyume.Model.EnumType;
    using TableData;
    using UniRx;
    
    public class LoginScene : BaseScene
    {
        // TODO: 기존 introScreen 활성화 여부 호환을 위해 추가. 추후 제거
        public static bool IsOnIntroScene { get; private set; } = true;
        
        [SerializeField] private IntroScreen introScreen;
        [SerializeField] private Synopsis synopsis;

#region MonoBehaviour
        private void Awake()
        {
            synopsis.LoginScene = this;
            var canvas = GetComponent<Canvas>();
            canvas.worldCamera = ActionCamera.instance.Cam;
        }

        protected override void Start()
        {
            base.Start();
            StartCoroutine(LoginStart());
        }
#endregion MonoBehaviour

#region BaseScene
        protected override async UniTask LoadSceneAssets()
        {
            await UniTask.CompletedTask;
        }

        protected override async UniTask WaitActionResponse()
        {
            await UniTask.CompletedTask;
        }

        public override void Clear()
        {
            IsOnIntroScene = false;
            introScreen.Close();
            synopsis.Close();
        }
#endregion BaseScene

        private IEnumerator LoginStart()
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
            yield return StartCoroutine(CoLogin(planetContext, succeed =>
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
            introScreen.Close();
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
                    await EnterGame(slotIndex);
                    loadingScreen.Close();
                }
                else
                {
                    synopsis.Show();
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
                        EnterCharacterSelect();
                    }
                    else
                    {
                        await EnterGame(slotIndex);
                    }
                }
                else
                {
                    EnterCharacterSelect();
                }
            }

            Widget.Find<GrayLoadingScreen>().Close();
        }

        public static async UniTask EnterCharacterSelect()
        {
            NcDebug.Log("[LoginScene] EnterCharacterSelect() invoked");
            
            await NcSceneManager.Instance.LoadScene(SceneType.Game);
            
            // TODO: ChangeScene
            Widget.Find<Login>().Show();
            Event.OnNestEnter.Invoke();
        }
        
        /// <summary>
        /// 로컬에 저장된 아바타 정보가 있는 경우, 특정 상황에서 바로 해당 아바타 선택 후 게임 로비 진입
        /// </summary>
        public async UniTask EnterGame(int slotIndex, bool forceNewSelection = true)
        {
            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            await RxProps.SelectAvatarAsync(slotIndex, Game.instance.Agent.BlockTipStateRootHash, forceNewSelection);
            sw.Stop();
            NcDebug.Log("[LoginScene] EnterNext()... SelectAvatarAsync() finished in" +
                $" {sw.ElapsedMilliseconds}ms.(elapsed)");
            
            await NcSceneManager.Instance.LoadScene(SceneType.Game);
            
            Event.OnRoomEnter.Invoke(false);
            Event.OnUpdateAddresses.Invoke();
        }

        private IEnumerator CoLogin(PlanetContext planetContext, Action<bool> loginCallback)
        {
            var game = Game.instance;
            var commandLineOptions = game.CommandLineOptions;
            
            NcDebug.Log("[LoginScene] CoLogin() invoked");
            if (commandLineOptions.Maintenance)
            {
                ShowMaintenancePopup();
                yield break;
            }

            if (commandLineOptions.TestEnd)
            {
                ShowTestEnd();
                yield break;
            }

            var loginSystem = Widget.Find<LoginSystem>();
            var dimmedLoadingScreen = Widget.Find<DimmedLoadingScreen>();
            var sw = new Stopwatch();
            if (Application.isBatchMode)
            {
                loginSystem.Show(commandLineOptions.PrivateKey);
                yield return game.AgentInitialize(false, loginCallback);
                yield break;
            }

#if !RUN_ON_MOBILE
            // NOTE: planetContext and planet info are already initialized when the game is launched from the non-mobile platform.
            NcDebug.Log("[LoginScene] CoLogin()... PlanetContext and planet info are already initialized.");
            if (!KeyManager.Instance.IsSignedIn)
            {
                NcDebug.Log("[LoginScene] CoLogin()... KeyManager.Instance.IsSignedIn is false");
                if (!KeyManager.Instance.TrySigninWithTheFirstRegisteredKey())
                {
                    NcDebug.Log("[LoginScene] CoLogin()... LoginSystem.TryLoginWithLocalPpk() is false.");
                    introScreen.Show(
                        commandLineOptions.KeyStorePath,
                        commandLineOptions.PrivateKey,
                        null);
                }

                NcDebug.Log("[LoginScene] CoLogin()... WaitUntil KeyManager.Instance.IsSignedIn.");
                yield return new WaitUntil(() => KeyManager.Instance.IsSignedIn);
                NcDebug.Log("[LoginScene] CoLogin()... WaitUntil KeyManager.Instance.IsSignedIn. Done.");

                // NOTE: Update CommandlineOptions.PrivateKey finally.
                commandLineOptions.PrivateKey = KeyManager.Instance.SignedInPrivateKey.ToHexWithZeroPaddings();
                NcDebug.Log("[LoginScene] CoLogin()... CommandLineOptions.PrivateKey finally updated" +
                    $" to ({KeyManager.Instance.SignedInAddress}).");
            }

            yield return game.AgentInitialize(true, loginCallback);
            yield break;
#endif

            // NOTE: Initialize current planet info.
            sw.Reset();
            sw.Start();
            planetContext = PlanetSelector.InitializeSelectedPlanetInfo(planetContext);
            sw.Stop();
            NcDebug.Log($"[LoginScene] CoLogin()... PlanetInfo selected in {sw.ElapsedMilliseconds}ms.(elapsed)");

            if (planetContext.HasError)
            {
                loginCallback.Invoke(false);
                yield break;
            }

            yield return CheckAlreadyLoginOrLocalPassphrase(planetContext);
            if (planetContext.HasError)
            {
                // TODO: UniTask등을 이용해서 리턴값을 받아서 처리하는 방법으로 변경할 수 있음 좋을듯
                loginCallback.Invoke(false);
                yield break;
            }

            if (planetContext.HasPledgedAccount)
            {
                yield return HasPledgedAccountProcess(planetContext, loginCallback);
                yield break;
            }

            // NOTE: Show IntroScreen's tab to start button.
            //       It should be called after the PlanetSelector.InitializeSelectedPlanetInfo().
            //       Because the IntroScreen uses the PlanetContext.SelectedPlanetInfo.
            //       And it should be called after the IntroScreen.SetData().
            introScreen.ShowTabToStart();
            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnClickTabToStart.");
            yield return introScreen.OnClickTabToStart.AsObservable().First().StartAsCoroutine();
            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnClickTabToStart. Done.");

            string email = null;
            Address? agentAddrInPortal = null;
            if (SigninContext.HasLatestSignedInSocialType)
            {
                yield return HasLatestSignedInSocialTypeProcess((outEmail, outAddress) =>
                {
                    email = outEmail;
                    agentAddrInPortal = outAddress;
                });
            }
            else
            {
                // NOTE: Social login flow.
                NcDebug.Log("[LoginScene] CoLogin()... Go to social login flow.");
                var socialType = SigninContext.SocialType.Apple;
                string idToken = null;
                introScreen.OnSocialSignedIn.AsObservable()
                    .First()
                    .Subscribe(value =>
                    {
                        socialType = value.socialType;
                        email = value.email;
                        idToken = value.idToken;
                    });

                NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnSocialSignedIn.");
                yield return new WaitUntil(() => idToken is not null || KeyManager.Instance.IsSignedIn);
                NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnSocialSignedIn. Done.");

                // Guest private key login flow
                if (KeyManager.Instance.IsSignedIn)
                {
                    yield return game.AgentInitialize(false, loginCallback);
                    yield break;
                }

                yield return PortalLoginProcess(socialType, idToken, outAddr => { agentAddrInPortal = outAddr; });
            }

            // NOTE: Update PlanetContext.PlanetAccountInfos.
            if (agentAddrInPortal is null)
            {
                NcDebug.Log("[LoginScene] CoLogin()... AgentAddress in portal is null");
                if (!KeyManager.Instance.IsSignedIn)
                {
                    NcDebug.Log("[LoginScene] CoLogin()... KeyManager.Instance.IsSignedIn is false");
                    loginSystem.Show(connectedAddress: null);
                    // NOTE: Don't set the autoGeneratedAgentAddress to agentAddr.
                    var autoGeneratedAgentAddress = KeyManager.Instance.SignedInAddress;
                    NcDebug.Log($"[LoginScene] CoLogin()... auto generated agent address: {autoGeneratedAgentAddress}." +
                        " And Update planet account infos w/ empty agent address.");
                }

                // NOTE: Initialize planet account infos as default(empty) value
                //       when agent address is not set.
                planetContext.PlanetAccountInfos = planetContext.PlanetRegistry?.PlanetInfos
                    .Select(planetInfo => new PlanetAccountInfo(
                        planetInfo.ID,
                        null,
                        null))
                    .ToArray();
            }
            else
            {
                var requiredAddress = agentAddrInPortal.Value;
                NcDebug.Log($"[LoginScene] CoLogin()... AgentAddress({requiredAddress}) in portal" +
                    $" is not null. Try to update planet account infos.");
                dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPlanetAccountInfoSyncing);
                yield return PlanetSelector.UpdatePlanetAccountInfosAsync(
                    planetContext,
                    requiredAddress,
                    false).ToCoroutine();
                dimmedLoadingScreen.Close();
                if (planetContext.HasError)
                {
                    loginCallback?.Invoke(false);
                    yield break;
                }
            }

            // NOTE: Check if the planets have at least one agent.
            if (planetContext.HasPledgedAccount)
            {
                yield return ShowPlanetAccountInfosPopup(planetContext);

                if (planetContext.IsSelectedPlanetAccountPledged)
                {
                    IsSelectedPlanetAccountPledgedProcess();
                }
                else
                {
                    IsNotSelectedPlanetAccountPledgedProcess();
                }

                NcDebug.Log("[LoginScene] CoLogin()... WaitUntil KeyManager.Instance.IsSignedIn.");
                yield return new WaitUntil(() => KeyManager.Instance.IsSignedIn);
                NcDebug.Log("[LoginScene] CoLogin()... WaitUntil KeyManager.Instance.IsSignedIn. Done.");
            }
            else
            {
                NcDebug.Log("[LoginScene] CoLogin()... pledged account not exist.");
                if (!KeyManager.Instance.IsSignedIn)
                {
                    HasNotPledgedAccountAndNotSignedProcess(planetContext, email, agentAddrInPortal);
                    loginCallback?.Invoke(false);
                    yield break;
                }

                NcDebug.Log("[LoginScene] CoLogin()... Player have to make a pledge.");
                NcDebug.Log("[LoginScene] CoLogin()... Set planetContext.SelectedPlanetAccountInfo" +
                    " w/ planetContext.SelectedPlanetInfo.ID.");
                planetContext.SelectedPlanetAccountInfo = planetContext.PlanetAccountInfos!.First(e =>
                    e.PlanetId.Equals(planetContext.SelectedPlanetInfo!.ID));
            }

            game.CurrentSocialEmail = email ?? string.Empty;

            // NOTE: Update CommandlineOptions.PrivateKey finally.
            commandLineOptions.PrivateKey = KeyManager.Instance.SignedInPrivateKey.ToHexWithZeroPaddings();
            NcDebug.Log("[LoginScene] CoLogin()... CommandLineOptions.PrivateKey finally updated" +
                $" to ({KeyManager.Instance.SignedInAddress}).");

            yield return game.AgentInitialize(true, loginCallback);
        }

        private void ShowMaintenancePopup()
        {
            var w = Widget.Create<IconAndButtonSystem>();
            w.ConfirmCallback = () => Application.OpenURL(LiveAsset.GameConfig.DiscordLink);
            if (Nekoyume.Helper.Util.GetKeystoreJson() != string.Empty)
            {
                w.SetCancelCallbackToBackup();
                w.ShowWithTwoButton("UI_MAINTENANCE",
                    "UI_MAINTENANCE_CONTENT",
                    "UI_OK",
                    "UI_KEY_BACKUP",
                    true,
                    IconAndButtonSystem.SystemType.Information
                );
            }
            else
            {
                w.Show("UI_MAINTENANCE",
                    "UI_MAINTENANCE_CONTENT",
                    "UI_OK",
                    true,
                    IconAndButtonSystem.SystemType.Information);
            }
        }

        private void ShowTestEnd()
        {
            var w = Widget.Find<ConfirmPopup>();
            w.CloseCallback = result =>
            {
                if (result == ConfirmResult.Yes)
                {
                    Application.OpenURL(LiveAsset.GameConfig.DiscordLink);
                }

                Game.ApplicationQuit();
            };
            w.Show("UI_TEST_END", "UI_TEST_END_CONTENT", "UI_GO_DISCORD", "UI_QUIT");
        }

        private IEnumerator CheckAlreadyLoginOrLocalPassphrase(PlanetContext planetContext)
        {
            var commandLineOptions = Game.instance.CommandLineOptions;
            if (KeyManager.Instance.IsSignedIn || KeyManager.Instance.TrySigninWithTheFirstRegisteredKey())
            {
                NcDebug.Log("[LoginScene] CoLogin()... KeyManager.Instance.IsSignedIn is true or" +
                    " LoginSystem.TryLoginWithLocalPpk() is true.");
                var pk = KeyManager.Instance.SignedInPrivateKey;

                // NOTE: Update CommandlineOptions.PrivateKey.
                commandLineOptions.PrivateKey = pk.ToHexWithZeroPaddings();
                NcDebug.Log("[LoginScene] CoLogin()... CommandLineOptions.PrivateKey updated" +
                    $" to ({pk.Address}).");

                // NOTE: Check PlanetContext.CanSkipPlanetSelection.
                //       If true, then update planet account infos for IntroScreen.
                if (planetContext.CanSkipPlanetSelection.HasValue && planetContext.CanSkipPlanetSelection.Value)
                {
                    NcDebug.Log("[LoginScene] CoLogin()... PlanetContext.CanSkipPlanetSelection is true.");
                    var dimmedLoadingScreen = Widget.Find<DimmedLoadingScreen>();
                    dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPlanetAccountInfoSyncing);
                    yield return PlanetSelector.UpdatePlanetAccountInfosAsync(
                        planetContext,
                        pk.Address,
                        true).ToCoroutine();
                    dimmedLoadingScreen.Close();
                    if (planetContext.HasError)
                    {
                        // TODO: UniTask등을 이용해서 리턴값을 받아서 처리하는 방법으로 변경할 수 있음 좋을듯
                        yield break;
                    }
                }

                // TODO: UniTask등을 이용해서 리턴값을 받아서 처리하는 방법으로 변경할 수 있음 좋을듯
                introScreen.SetData(commandLineOptions.KeyStorePath,
                    pk.ToHexWithZeroPaddings(),
                    planetContext);
            }
            else
            {
                NcDebug.Log("[LoginScene] CoLogin()... LocalSystem.Login is false.");
                // NOTE: If we need to cover the Multiplanetary context on non-mobile platform,
                //       we need to reconsider the invoking the IntroScreen.Show(pkPath, pk, planetContext)
                //       in here.
                introScreen.SetData(
                    commandLineOptions.KeyStorePath,
                    commandLineOptions.PrivateKey,
                    planetContext);
            }
        }

        private IEnumerator HasPledgedAccountProcess(PlanetContext planetContext, Action<bool> loginCallback)
        {
            var game = Game.instance;
            var commandLineOptions = game.CommandLineOptions;
            
            NcDebug.Log("[LoginScene] CoLogin()... Has pledged account.");
            var pk = KeyManager.Instance.SignedInPrivateKey;
            introScreen.Show(commandLineOptions.KeyStorePath,
                pk.ToHexWithZeroPaddings(),
                planetContext);

            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnClickStart.");
            yield return introScreen.OnClickStart.AsObservable().First().StartAsCoroutine();
            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnClickStart. Done.");

            // NOTE: Update CommandlineOptions.PrivateKey finally.
            commandLineOptions.PrivateKey = pk.ToHexWithZeroPaddings();
            NcDebug.Log("[LoginScene] CoLogin()... CommandLineOptions.PrivateKey finally updated" +
                $" to ({pk.Address}).");

            yield return game.AgentInitialize(true, loginCallback);
        }

        /// <summary>
        /// If has latest signed in social type, then return email and agent address in portal.
        /// </summary>
        /// <param name="outValueCallback">callback of (email, agentAddrInPortal)</param>
        private IEnumerator HasLatestSignedInSocialTypeProcess(Action<string, Address?> outValueCallback)
        {
            var game = Game.instance;
            var dimmedLoadingScreen = Widget.Find<DimmedLoadingScreen>();
            dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPortalAuthenticating);

            var sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            var getTokensTask = game.PortalConnect.GetTokensSilentlyAsync();
            yield return new WaitUntil(() => getTokensTask.IsCompleted);
            sw.Stop();
            NcDebug.Log($"[LoginScene] CoLogin()... Portal signed in in {sw.ElapsedMilliseconds}ms.(elapsed)");
            dimmedLoadingScreen.Close();
            outValueCallback?.Invoke(getTokensTask.Result.email, getTokensTask.Result.address);

            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnClickStart.");
            yield return introScreen.OnClickStart.AsObservable().First().StartAsCoroutine();
            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnClickStart. Done.");
        }

        private IEnumerator PortalLoginProcess(SigninContext.SocialType socialType, string idToken, Action<Address?> callback)
        {
            var game = Game.instance;
            var sw = new Stopwatch();
            var dimmedLoadingScreen = Widget.Find<DimmedLoadingScreen>();

            // NOTE: Portal login flow.
            dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPortalAuthenticating);
            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil PortalConnect.Send{Apple|Google}IdTokenAsync.");
            sw.Reset();
            sw.Start();
            var portalSigninTask = socialType == SigninContext.SocialType.Apple
                ? game.PortalConnect.SendAppleIdTokenAsync(idToken)
                : game.PortalConnect.SendGoogleIdTokenAsync(idToken);
            yield return new WaitUntil(() => portalSigninTask.IsCompleted);
            sw.Stop();
            NcDebug.Log($"[LoginScene] CoLogin()... Portal signed in in {sw.ElapsedMilliseconds}ms.(elapsed)");
            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil PortalConnect.Send{Apple|Google}IdTokenAsync. Done.");
            dimmedLoadingScreen.Close();

            callback?.Invoke(portalSigninTask.Result);
        }

        private IEnumerator ShowPlanetAccountInfosPopup(PlanetContext planetContext)
        {
            NcDebug.Log("[LoginScene] CoLogin()... Has pledged account. Show planet account infos popup.");
            introScreen.ShowPlanetAccountInfosPopup(planetContext, !KeyManager.Instance.IsSignedIn);

            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil planetContext.SelectedPlanetAccountInfo is not null.");
            yield return new WaitUntil(() => planetContext.SelectedPlanetAccountInfo is not null);
            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil planetContext.SelectedPlanetAccountInfo" +
                $" is not null. Done. {planetContext.SelectedPlanetAccountInfo!.PlanetId}");
        }

        private void IsSelectedPlanetAccountPledgedProcess()
        {
            // NOTE: Player selected the planet that has agent.
            NcDebug.Log("[LoginScene] CoLogin()... Try to import key w/ QR code." +
                " Player don't have to make a pledge.");

            // NOTE: Complex logic here...
            //       - LoginSystem.Login is false.
            //       - Portal has player's account.
            //       - Click the IntroScreen.AgentInfo.accountImportKeyButton.
            //         - Import the agent key.
            if (!KeyManager.Instance.IsSignedIn)
            {
                // NOTE: QR code import sets KeyManager.Instance.IsSignedIn to true.
                introScreen.ShowForQrCodeGuide();
            }
        }

        private void IsNotSelectedPlanetAccountPledgedProcess()
        {
            // NOTE: Player selected the planet that has no agent.
            NcDebug.Log("[LoginScene] CoLogin()... Try to create a new agent." +
                " Player may have to make a pledge.");

            // NOTE: Complex logic here...
            //       - LoginSystem.Login is false.
            //       - Portal has player's account.
            //       - Click the IntroScreen.AgentInfo.noAccountCreateButton.
            //         - Create a new agent in a new planet.
            if (!KeyManager.Instance.IsSignedIn)
            {
                // NOTE: QR code import sets KeyManager.Instance.IsSignedIn to true.
                introScreen.ShowForQrCodeGuide();
            }
        }

        private void HasNotPledgedAccountAndNotSignedProcess(PlanetContext planetContext, string email, Address? agentAddrInPortal)
        {
            var portalConnect = Game.instance.PortalConnect;
            NcDebug.Log("[LoginScene] CoLogin()... KeyManager.Instance.IsSignedIn is false");

            // FIXME: 이 분기의 상황
            //        - 포탈에는 에이전트 A의 주소가 있다.
            //        - 플래닛 레지스트리를 기준으로 에이전트 A는 아직 플렛지한 플래닛이 없다.
            //        - 로컬에 에이전트 키가 없어서 로그인되지 않았다.
            //
            //        그래서 할 일.
            //        - 로컬 키스토어를 순회하면서 에이전트 A의 주소와 같은 키를 찾는다.
            //        - 포탈에 등록된 에이전트 A의 주소를 보여주면서, 이에 대응하는 키를 QR로 가져오라고 안내한다.
            //        - 등...
            //
            // FIXME: States of here.
            //        - Portal has agent address A which connected with social account.
            //        - No planet which pledged by agent address A in the planet registry.
            //        - LoginSystem.Login is false because of no agent key in the local.
            //
            //        So, we have to do.
            //        - Find the agent key which has same address with agent address A in the local.
            //        - While showing the agent address A, request to import the agent key which is
            //          corresponding to the agent address A.
            //        - etc...

            NcDebug.LogError("Portal has agent address which connected with social account." +
                " But no agent states in the all planets." +
                $"\n Portal: {portalConnect.PortalUrl}" +
                $"\n Social Account: {email}" +
                $"\n Agent Address in portal: {agentAddrInPortal}");
            planetContext.SetError(
                PlanetContext.ErrorType.UnsupportedCase01,
                portalConnect.PortalUrl,
                email,
                agentAddrInPortal?.ToString() ?? "null");
        }
    }
}
