#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define RUN_ON_MOBILE
#define ENABLE_FIREBASE
#endif
#if !UNITY_EDITOR && UNITY_STANDALONE
#define RUN_ON_STANDALONE
#endif

using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Game.Character;
using Nekoyume.Multiplanetary;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if ENABLE_FIREBASE
using NineChronicles.GoogleServices.Firebase.Runtime;
#endif

namespace Nekoyume.Game.Scene
{
    using UniRx;

    public class LoginScene : BaseScene
    {
        // TODO: 기존 introScreen 활성화 여부 호환을 위해 추가. 추후 제거
        public static bool IsOnIntroScene { get; private set; } = true;

        [SerializeField] private IntroScreen introScreen;
        [SerializeField] private Synopsis synopsis;
        [SerializeField] private ChainInfoItem thorChainInfoItem;

#region MonoBehaviour
        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.worldCamera = ActionCamera.instance.Cam;

            Nekoyume.Game.LiveAsset.LiveAssetManager.instance.OnChangedThorSchedule +=
                OnSetThorScheduleUrl;
        }

        protected override void Start()
        {
            base.Start();
            StartCoroutine(LoginStart());
        }

        private void OnDestroy()
        {
            Nekoyume.Game.LiveAsset.LiveAssetManager.instance.OnChangedThorSchedule -=
                OnSetThorScheduleUrl;
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

        private CommandLineOptions CommandLineOptions => Game.instance.CommandLineOptions;

        private IEnumerator LoginStart()
        {
            var game = Game.instance;
            yield return ResourceManager.Instance.InitializeAsync().ToCoroutine();

#if LIB9C_DEV_EXTENSIONS && UNITY_ANDROID
            Lib9c.DevExtensions.TestbedHelper.LoadTestbedCreateAvatarForQA();
#endif
            var totalSw = new Stopwatch();
            totalSw.Start();

            game.AddRequestManager();
            yield return game.InitializeLiveAssetManager();
            // if Mobile Build, need refresh commandLineOptions

            // NOTE: Initialize KeyManager after load CommandLineOptions.
            if (!KeyManager.Instance.IsInitialized)
            {
                KeyManager.Instance.Initialize(
                    CommandLineOptions.KeyStorePath,
                    Helper.Util.AesEncrypt,
                    Helper.Util.AesDecrypt);
            }

            // NOTE: Try to sign in with the first registered key
            //       if the CommandLineOptions.PrivateKey is empty in mobile.
            if (Platform.IsMobilePlatform() &&
                string.IsNullOrEmpty(CommandLineOptions.PrivateKey) &&
                KeyManager.Instance.TrySigninWithTheFirstRegisteredKey())
            {
                NcDebug.Log("[LoginScene] Start()... CommandLineOptions.PrivateKey is empty in mobile." +
                    " Set cached private key instead.");
                CommandLineOptions.PrivateKey =
                    KeyManager.Instance.SignedInPrivateKey.ToHexWithZeroPaddings();
            }

            yield return game.InitializeL10N();
            yield return L10nManager.AdditionalL10nTableDownload("https://assets.nine-chronicles.com/live-assets/Csv/RemoteCsv.csv").ToCoroutine();
            NcDebug.Log("[Game] Start()... L10nManager initialized");

            // NOTE: Initialize planet registry.
            //       It should do after load CommandLineOptions.
            //       And it should do before initialize Agent.
            var planetContext = new PlanetContext(CommandLineOptions);
            yield return planetContext.InitializePlanetContextAsync().ToCoroutine();

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
            else if (planetContext.PlanetRegistry!.TryGetPlanetInfoByHeadlessGrpc(CommandLineOptions.RpcServerHost, out var planetInfo))
            {
                NcDebug.Log("[LoginScene] UpdateCurrentPlanetIdAsync()... planet id is found in planet registry.");
                game.CurrentPlanetId = planetInfo.ID;
            }
            else if (!string.IsNullOrEmpty(CommandLineOptions.SelectedPlanetId))
            {
                NcDebug.Log("[LoginScene] UpdateCurrentPlanetIdAsync()... SelectedPlanetId is not null.");
                game.CurrentPlanetId = new PlanetId(CommandLineOptions.SelectedPlanetId);
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
                CommandLineOptions.PrivateKey is null
                    ? null
                    : PrivateKey.FromString(CommandLineOptions.PrivateKey).Address,
                null,
                CommandLineOptions.RpcServerHost);
            game.Analyzer.Track("Unity/Started");

            game.InitializeMessagePackResolver();

            if (game.CheckRequiredUpdate())
            {
                // NOTE: Required update is detected.
                yield break;
            }

            // NOTE: Apply l10n to IntroScreen after L10nManager initialized.
            game.InitializeFirstResources();

            // AudioController 초기화
            // 에디터의 경우 에셋 로드 속도(use asset database)가 비정상적으로 느리기 때문에
            // AudioController의 초기화를 대기하지 않고 아래 로직을 수행한다
            // AudioController가 초기화되지 않은 상태에서 사운드 재생시 안내 로그 출력 후 사운드가 재생되지 않음 
#if UNITY_EDITOR
            game.InitializeAudioControllerAsync().Forget();
#else
            yield return game.InitializeAudioControllerAsync().ToCoroutine();
#endif

            // NOTE: Initialize IAgent.
            var agentInitialized = false;
            var agentInitializeSucceed = false;
            yield return StartCoroutine(CoLogin(planetContext, succeed =>
                    {
                        NcDebug.Log($"[LoginScene] Agent initialized. {succeed}");
                        agentInitialized = true;
                        agentInitializeSucceed = succeed;
                    }
                ).ToCoroutine()
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
            ApiClients.Instance.Initialize(CommandLineOptions);

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
            }

            if (!planetContext.IsSelectedPlanetAccountPledged)
            {
                yield return StartCoroutine(game.CoCheckPledge(planetContext.SelectedPlanetInfo.ID));
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

            yield return game.InitializeStage().ToCoroutine();

            // Initialize Rank.SharedModel
            RankPopup.UpdateSharedModel();
            Helper.Util.TryGetAppProtocolVersionFromToken(
                CommandLineOptions.AppProtocolVersion,
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
            Widget.Find<GrayLoadingScreen>().Close();
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
                    await EnterGame(slotIndex);
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
                        await EnterCharacterSelect();
                    }
                    else
                    {
                        await EnterGame(slotIndex);
                    }
                }
                else
                {
                    await EnterCharacterSelect();
                }
            }
        }

        public static async UniTask EnterCharacterSelect()
        {
            NcDebug.Log("[LoginScene] EnterCharacterSelect() invoked");

            var loadingScreen = Widget.Find<LoadingScreen>();
            loadingScreen.Show(
                LoadingScreen.LoadingType.Entering,
                L10nManager.Localize("UI_LOADING_BOOTSTRAP_START"));
            await NcSceneManager.Instance.LoadScene(SceneType.Game);
            loadingScreen.Close();

            // TODO: ChangeScene
            Widget.Find<Login>().Show();
            Event.OnNestEnter.Invoke();
        }

        /// <summary>
        /// 로컬에 저장된 아바타 정보가 있는 경우, 특정 상황에서 바로 해당 아바타 선택 후 게임 로비 진입
        /// </summary>
        public static async UniTask EnterGame(int slotIndex, bool forceNewSelection = true)
        {
            var sw = new Stopwatch();
            sw.Reset();
            var loadingScreen = Widget.Find<LoadingScreen>();
            loadingScreen.Show(
                LoadingScreen.LoadingType.Entering,
                L10nManager.Localize("UI_LOADING_BOOTSTRAP_START"));
            sw.Start();
            await RxProps.SelectAvatarAsync(slotIndex, Game.instance.Agent.BlockTipStateRootHash, forceNewSelection);
            sw.Stop();
            NcDebug.Log($"[LoginScene] EnterNext()... SelectAvatarAsync() finished in {sw.ElapsedMilliseconds}ms.(elapsed)");

            await NcSceneManager.Instance.LoadScene(SceneType.Game);
            loadingScreen.Close();

            Lobby.Enter();
            Event.OnUpdateAddresses.Invoke();
        }

        private async UniTask CoLogin(PlanetContext planetContext, Action<bool> loginCallback)
        {
            var game = Game.instance;

            NcDebug.Log("[LoginScene] CoLogin() invoked");
            if (CommandLineOptions.Maintenance)
            {
                ShowMaintenancePopup();
                return;
            }

            if (CommandLineOptions.TestEnd)
            {
                ShowTestEnd();
                return;
            }

            var loginSystem = Widget.Find<LoginSystem>();
            if (Application.isBatchMode)
            {
                loginSystem.Show(CommandLineOptions.PrivateKey);
                await game.AgentInitialize(false, loginCallback).ToUniTask();
                return;
            }

#if !RUN_ON_MOBILE
            await CoLoginPc(loginCallback);
            return;
#endif
            await CoLoginMobile(planetContext, loginCallback);
        }

        private async UniTask CoLoginPc(Action<bool> loginCallback)
        {
            var game = Game.instance;
            // NOTE: planetContext and planet info are already initialized when the game is launched from the non-mobile platform.
            NcDebug.Log("[LoginScene] CoLoginPc()... PlanetContext and planet info are already initialized.");

            if (!KeyManager.Instance.IsSignedIn)
            {
                NcDebug.Log("[LoginScene] CoLoginPc()... KeyManager.Instance.IsSignedIn is false");
                if (!KeyManager.Instance.TrySigninWithTheFirstRegisteredKey())
                {
                    NcDebug.Log("[LoginScene] CoLoginPc()... LoginSystem.TryLoginWithLocalPpk() is false.");
                    introScreen.Show(
                        CommandLineOptions.KeyStorePath,
                        CommandLineOptions.PrivateKey,
                        null);
                }

                NcDebug.Log("[LoginScene] CoLoginPc()... WaitUntil KeyManager.Instance.IsSignedIn.");
                await UniTask.WaitUntil(() => KeyManager.Instance.IsSignedIn);
                NcDebug.Log("[LoginScene] CoLoginPc()... WaitUntil KeyManager.Instance.IsSignedIn. Done.");

                // NOTE: Update CommandlineOptions.PrivateKey finally.
                CommandLineOptions.PrivateKey = KeyManager.Instance.SignedInPrivateKey.ToHexWithZeroPaddings();
                NcDebug.Log($"[LoginScene] CoLoginPc()... CommandLineOptions.PrivateKey updated to ({KeyManager.Instance.SignedInAddress}).");
            }

            await game.AgentInitialize(true, loginCallback).ToUniTask();
        }

        private async UniTask CoLoginMobile(PlanetContext planetContext, Action<bool> loginCallback)
        {
            var game = Game.instance;
            var loginSystem = Widget.Find<LoginSystem>();
            var dimmedLoadingScreen = Widget.Find<DimmedLoadingScreen>();
            var sw = new Stopwatch();

            planetContext = InitializePlanetInfo(planetContext, sw);

            if (planetContext.HasError)
            {
                loginCallback.Invoke(false);
                return;
            }

            if (!await CheckLoginOrPassphrase(planetContext, loginCallback))
            {
                return;
            }

            if (planetContext.HasPledgedAccount)
            {
                await HasPledgedAccountProcess(planetContext, loginCallback).ToUniTask();
                return;
            }

            await ShowIntroAndWaitForStart();

            string email = null;
            Address? agentAddrInPortal = null;
            if (SigninContext.HasLatestSignedInSocialType)
            {
                await HasLatestSignedInSocialTypeProcess((outEmail, outAddress) =>
                {
                    email = outEmail;
                    agentAddrInPortal = outAddress;
                }).ToUniTask();
            }
            else
            {
                // Social login flow
                NcDebug.Log("[LoginScene] CoLoginMobile()... Go to social login flow.");
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

                NcDebug.Log("[LoginScene] CoLoginMobile()... WaitUntil introScreen.OnSocialSignedIn.");
                await UniTask.WaitUntil(() => idToken is not null || KeyManager.Instance.IsSignedIn);
                NcDebug.Log("[LoginScene] CoLoginMobile()... WaitUntil introScreen.OnSocialSignedIn. Done.");

                // Guest private key login flow
                if (KeyManager.Instance.IsSignedIn)
                {
                    await game.AgentInitialize(false, loginCallback).ToUniTask();
                    return;
                }

                await PortalLoginProcess(socialType, idToken, outAddr => { agentAddrInPortal = outAddr; }).ToUniTask();
            }

            if (!await UpdatePlanetAccountInfos(planetContext, loginSystem, dimmedLoadingScreen, agentAddrInPortal, loginCallback))
            {
                return;
            }

            if (!await CheckPledgedAccount(planetContext, loginCallback, email, agentAddrInPortal))
            {
                return;
            }

            game.CurrentSocialEmail = email ?? string.Empty;

            // Update CommandlineOptions.PrivateKey finally.
            CommandLineOptions.PrivateKey = KeyManager.Instance.SignedInPrivateKey.ToHexWithZeroPaddings();
            NcDebug.Log($"[LoginScene] CoLoginMobile()... CommandLineOptions.PrivateKey updated to ({KeyManager.Instance.SignedInAddress}).");

            await game.AgentInitialize(true, loginCallback).ToUniTask();
        }

        private PlanetContext InitializePlanetInfo(PlanetContext planetContext, Stopwatch sw)
        {
            sw.Reset();
            sw.Start();
            planetContext = PlanetSelector.InitializeSelectedPlanetInfo(planetContext);
            sw.Stop();
            NcDebug.Log($"[LoginScene] CoLoginMobile()... PlanetInfo selected in {sw.ElapsedMilliseconds}ms.(elapsed)");

            return planetContext;
        }

        private async UniTask<bool> CheckLoginOrPassphrase(PlanetContext planetContext, Action<bool> loginCallback)
        {
            await CheckAlreadyLoginOrLocalPassphrase(planetContext).ToUniTask();
            if (!planetContext.HasError)
            {
                return true;
            }

            loginCallback.Invoke(false);
            return false;
        }

        private async UniTask ShowIntroAndWaitForStart()
        {
            introScreen.ShowTabToStart();
            introScreen.ShowTabToStart(); // 의도적으로 두 번 호출되어 있는 것으로 보입니다.
            NcDebug.Log("[LoginScene] CoLoginMobile()... WaitUntil introScreen.OnClickTabToStart.");
            await introScreen.OnClickTabToStart.AsObservable().First().ToUniTask();
            NcDebug.Log("[LoginScene] CoLoginMobile()... WaitUntil introScreen.OnClickTabToStart. Done.");
        }

        private async UniTask<bool> UpdatePlanetAccountInfos(
            PlanetContext planetContext,
            LoginSystem loginSystem,
            DimmedLoadingScreen dimmedLoadingScreen,
            Address? agentAddrInPortal,
            Action<bool> loginCallback
        )
        {
            if (agentAddrInPortal is null)
            {
                NcDebug.Log("[LoginScene] CoLoginMobile()... AgentAddress in portal is null");
                if (!KeyManager.Instance.IsSignedIn)
                {
                    NcDebug.Log("[LoginScene] CoLoginMobile()... KeyManager.Instance.IsSignedIn is false");
                    loginSystem.Show(connectedAddress: null);
                    var autoGeneratedAgentAddress = KeyManager.Instance.SignedInAddress;
                    NcDebug.Log($"[LoginScene] CoLogin()... auto generated agent address: {autoGeneratedAgentAddress}. And Update planet account infos w/ empty agent address.");
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
                NcDebug.Log($"[LoginScene] CoLoginMobile()... AgentAddress({requiredAddress}) in portal not null. Update planet account infos.");
                dimmedLoadingScreen.Show(DimmedLoadingScreen.ContentType.WaitingForPlanetAccountInfoSyncing);
                await PlanetSelector.UpdatePlanetAccountInfosAsync(
                    planetContext,
                    requiredAddress,
                    false,
                    context => { planetContext = context; }
                );
                dimmedLoadingScreen.Close();

                if (!planetContext.HasError)
                {
                    return true;
                }

                loginCallback?.Invoke(false);
                return false;
            }

            return true;
        }

        private async UniTask<bool> CheckPledgedAccount(
            PlanetContext planetContext,
            Action<bool> loginCallback,
            string email,
            Address? agentAddrInPortal
        )
        {
            if (planetContext.HasPledgedAccount)
            {
                await ShowPlanetAccountInfosPopup(planetContext).ToUniTask();

                if (planetContext.IsSelectedPlanetAccountPledged)
                {
                    IsSelectedPlanetAccountPledgedProcess();
                }
                else
                {
                    IsNotSelectedPlanetAccountPledgedProcess();
                }

                NcDebug.Log("[LoginScene] CoLoginMobile()... WaitUntil KeyManager.Instance.IsSignedIn.");
                await UniTask.WaitUntil(() => KeyManager.Instance.IsSignedIn);
                NcDebug.Log("[LoginScene] CoLoginMobile()... WaitUntil KeyManager.Instance.IsSignedIn. Done.");
            }
            else
            {
                NcDebug.Log("[LoginScene] CoLoginMobile()... pledged account not exist.");
                if (!KeyManager.Instance.IsSignedIn)
                {
                    HasNotPledgedAccountAndNotSignedProcess(planetContext, email, agentAddrInPortal);
                    loginCallback?.Invoke(false);
                    return false;
                }

                NcDebug.Log("[LoginScene] CoLoginMobile()... Player have to make a pledge.");
                NcDebug.Log("[LoginScene] CoLoginMobile()... Set planetContext.SelectedPlanetAccountInfo from PlanetAccountInfos.");
                planetContext.SelectedPlanetAccountInfo = planetContext.PlanetAccountInfos!.First(e =>
                    e.PlanetId.Equals(planetContext.SelectedPlanetInfo!.ID));
            }

            return true;
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
            if (KeyManager.Instance.IsSignedIn || KeyManager.Instance.TrySigninWithTheFirstRegisteredKey())
            {
                NcDebug.Log("[LoginScene] CoLogin()... KeyManager.Instance.IsSignedIn is true or" +
                    " LoginSystem.TryLoginWithLocalPpk() is true.");
                var pk = KeyManager.Instance.SignedInPrivateKey;

                // NOTE: Update CommandlineOptions.PrivateKey.
                CommandLineOptions.PrivateKey = pk.ToHexWithZeroPaddings();
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
                        true,
                        context => { planetContext = context;}).ToCoroutine();
                    dimmedLoadingScreen.Close();
                    if (planetContext.HasError)
                    {
                        // TODO: UniTask등을 이용해서 리턴값을 받아서 처리하는 방법으로 변경할 수 있음 좋을듯
                        yield break;
                    }
                }

                // TODO: UniTask등을 이용해서 리턴값을 받아서 처리하는 방법으로 변경할 수 있음 좋을듯
                introScreen.SetData(CommandLineOptions.KeyStorePath,
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
                    CommandLineOptions.KeyStorePath,
                    CommandLineOptions.PrivateKey,
                    planetContext);
            }
        }

        private IEnumerator HasPledgedAccountProcess(PlanetContext planetContext, Action<bool> loginCallback)
        {
            var game = Game.instance;

            NcDebug.Log("[LoginScene] CoLogin()... Has pledged account.");
            var pk = KeyManager.Instance.SignedInPrivateKey;
            introScreen.Show(CommandLineOptions.KeyStorePath,
                pk.ToHexWithZeroPaddings(),
                planetContext);

            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnClickStart.");
            yield return introScreen.OnClickStart.AsObservable().First().StartAsCoroutine();
            NcDebug.Log("[LoginScene] CoLogin()... WaitUntil introScreen.OnClickStart. Done.");

            // NOTE: Update CommandlineOptions.PrivateKey finally.
            CommandLineOptions.PrivateKey = pk.ToHexWithZeroPaddings();
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

        private void OnSetThorScheduleUrl(ThorSchedule thorSchedule)
        {
            thorChainInfoItem.gameObject.SetActive(thorSchedule?.IsOpened == true);
        }
    }
}
