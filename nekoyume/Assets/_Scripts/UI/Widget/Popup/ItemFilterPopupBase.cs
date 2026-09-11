using System;
using System.Collections.Generic;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    // System.ObservableExtensions.Subscribe 와 겹치므로 네임스페이스 안에서 import 한다
    // (L10nTextMeshProUGUI 등 코드베이스 관례).
    using UniRx;

    public struct ItemFilterOptions
    {
        /// <summary>
        /// 선택된 등급 번호들. 비어 있으면(또는 null) 등급 필터를 걸지 않은 것과 같다.
        /// </summary>
        /// <remarks>
        /// 아이템의 <c>Grade</c> 가 이미 int 이므로 번호를 그대로 담아 비교한다.
        /// 등급이 늘어도 이 타입은 그대로다.
        /// </remarks>
        public HashSet<int> Grades;

        public ItemFilterPopupBase.Elemental Elemental;
        public ItemFilterPopupBase.ItemType ItemType;
        public ItemFilterPopupBase.UpgradeLevel UpgradeLevel;
        public ItemFilterPopupBase.OptionCount OptionCount;
        public ItemFilterPopupBase.WithSkill WithSkill;

        public string SearchText;

        public bool IsNeedFilter =>
            Grades is { Count: > 0 } ||
            Elemental != ItemFilterPopupBase.Elemental.All ||
            ItemType != ItemFilterPopupBase.ItemType.All ||
            UpgradeLevel != ItemFilterPopupBase.UpgradeLevel.All ||
            OptionCount != ItemFilterPopupBase.OptionCount.All ||
            WithSkill != ItemFilterPopupBase.WithSkill.All;
    }

    public abstract class ItemFilterPopupBase : PopupWidget
    {
#region Internal Type

        /// <summary>
        /// 하위 등급을 한 토글로 묶는 상한. 이 값 이하가 "Below Epic" 한 칸에 들어간다.
        /// </summary>
        private const int BelowEpicMaxGrade = 2; // Normal + Rare

        private const string BelowEpicGradeKey = "UI_GRADE_BELOW_EPIC";

        /// <summary>등급 라벨 키. <c>UI_ITEM_GRADE_1</c> … 형태.</summary>
        private const string GradeKeyFormat = "UI_ITEM_GRADE_{0}";

        /// <summary>
        /// 등급 칸 수 상한. 등급 목록의 권위가 체인 시트라 오타 한 줄(<c>grade=88</c>)이
        /// 칸을 무한히 늘릴 수 있으므로 방어선을 둔다.
        /// </summary>
        private const int MaxGradeToggleCount = 16;

        [Flags]
        public enum Elemental
        {
            All = 0,
            Normal = 1 << 0,
            Fire = 1 << 1,
            Water = 1 << 2,
            Land = 1 << 3,
            Wind = 1 << 4
        }

        [Flags]
        public enum ItemType
        {
            All = 0,
            Weapon = 1 << 0,
            Armor = 1 << 1,
            Belt = 1 << 2,
            Necklace = 1 << 3,
            Ring = 1 << 4,
            Aura = 1 << 5,
            Grimoire = 1 << 6
        }

        [Flags]
        public enum UpgradeLevel
        {
            All = 0,
            Level0 = 1 << 0,
            Level1 = 1 << 1,
            Level2 = 1 << 2,
            Level3 = 1 << 3,
            Level4 = 1 << 4,
            Level5 = 1 << 5,
            Level6More = 1 << 6
        }

        [Flags]
        public enum OptionCount
        {
            All = 0,
            One = 1 << 0,
            Two = 1 << 1,
            Three = 1 << 2
        }

        [Flags]
        public enum WithSkill
        {
            All = 0,
            None = 1 << 0,
            With = 1 << 1
        }

        [Serializable]
        private abstract class ItemToggleType
        {
            public Toggle toggle;

            public abstract bool IsAll { get; }

            public abstract string GetOptionName { get; }

            public void ResetToAll()
            {
                if (toggle.isOn != IsAll)
                {
                    toggle.isOn = IsAll;
                }
            }

            public void OffAllToggle()
            {
                if (IsAll && toggle.isOn)
                {
                    toggle.isOn = false;
                }
            }
        }

        [Serializable]
        private class GradeToggle : ItemToggleType
        {
            /// <summary>
            /// 프리팹에 직렬화된 레거시 값. **0 이면 "전체" 토글**이라는 식별용으로만 쓴다.
            /// </summary>
            /// <remarks>
            /// 구 <c>[Flags] GradeFilterOption</c> 의 비트값(0/1/2/4/…/64)이 프리팹에 그대로
            /// 남아 있다. Unity 는 enum 을 int 로 직렬화하므로 타입만 int 로 바꾸면 기존
            /// 프리팹이 그대로 읽힌다 — 프리팹을 수정할 필요가 없다.
            /// 담당 등급은 <see cref="grades"/> 에 런타임 배정된다.
            /// </remarks>
            public int option;

            /// <summary>
            /// 이 토글이 담당하는 등급 번호들. 시트 기준으로 런타임에 배정된다.
            /// "전체" 토글과 미배정 토글은 비어 있다.
            /// </summary>
            [NonSerialized] public int[] grades = Array.Empty<int>();

            /// <summary>
            /// 라벨 L10N 키. 그룹 정의와 함께 배정되므로 라벨 규칙이 한 곳에만 있다.
            /// </summary>
            [NonSerialized] public string labelKey;

            /// <summary>
            /// 코드가 런타임에 만든 칸인가. 프리팹 칸은 디자이너가 정한 색을 그대로 두고,
            /// 복제 칸만 코드가 색을 정한다.
            /// </summary>
            [NonSerialized] public bool isCloned;

            public override bool IsAll => option == 0;

            /// <summary>
            /// 라벨. 셀의 <c>L10nTextMeshProUGUI</c> 에도 같은 키가 주입되므로
            /// (<c>ApplyLabelKey</c>) 언어를 바꿔도 결과가 같다.
            /// </summary>
            /// <remarks>
            /// 어떤 상태에서도 예외를 던지지 않아야 한다 — <c>BindToggleEvent</c> 가 모든
            /// 토글에 대해 이 값을 읽으므로, 미배정 토글 하나가 <c>Awake</c> 를 중단시키면
            /// 팝업의 버튼 배선까지 전부 건너뛰어진다.
            /// </remarks>
            public override string GetOptionName
            {
                get
                {
                    var key = IsAll ? string.Format(GradeKeyFormat, 0) : labelKey;
                    if (!string.IsNullOrEmpty(key) && L10nManager.ContainsKey(key))
                    {
                        return L10nManager.Localize(key);
                    }

                    // 시트가 클라보다 먼저 새 등급을 들고 올 수 있다(UI_ITEM_GRADE_9 미등록).
                    // 키가 없으면 화면에 "!UI_ITEM_GRADE_9!" 가 찍히므로 등급 번호로 떨어뜨린다.
                    return grades is { Length: > 0 } ? grades[0].ToString() : string.Empty;
                }
            }
        }

        [Serializable]
        private class ElementalToggle : ItemToggleType
        {
            public Elemental elemental;

            public override bool IsAll => elemental == Elemental.All;
            public override string GetOptionName => elemental.ToString();
        }

        [Serializable]
        private class ItemTypeToggle : ItemToggleType
        {
            public ItemType itemType;

            public override bool IsAll => itemType == ItemType.All;
            public override string GetOptionName => itemType.ToString();
        }

        [Serializable]
        private class UpgradeLevelToggle : ItemToggleType
        {
            public UpgradeLevel upgradeLevel;

            public override bool IsAll => upgradeLevel == UpgradeLevel.All;
            public override string GetOptionName => upgradeLevel switch
            {
                UpgradeLevel.All => "All",
                UpgradeLevel.Level0 => "+0",
                UpgradeLevel.Level1 => "+1",
                UpgradeLevel.Level2 => "+2",
                UpgradeLevel.Level3 => "+3",
                UpgradeLevel.Level4 => "+4",
                UpgradeLevel.Level5 => "+5",
                UpgradeLevel.Level6More => "+6 more",
                _ => upgradeLevel.ToString()
            };
        }

        [Serializable]
        private class OptionCountToggle : ItemToggleType
        {
            public OptionCount optionCount;

            public override bool IsAll => optionCount == OptionCount.All;
            public override string GetOptionName => optionCount.ToString();
        }

        [Serializable]
        private class WithSkillToggle : ItemToggleType
        {
            public WithSkill withSkill;

            public override bool IsAll => withSkill == WithSkill.All;
            public override string GetOptionName => withSkill.ToString();
        }

#endregion Internal Type

        [SerializeField]
        private List<GradeToggle> gradeToggles;

        [SerializeField]
        private List<ElementalToggle> elementalToggles;

        [SerializeField]
        private List<ItemTypeToggle> itemTypeToggles;

        [SerializeField]
        private List<UpgradeLevelToggle> upgradeLevelToggles;

        [SerializeField]
        private List<OptionCountToggle> optionCountToggles;

        [SerializeField]
        private List<WithSkillToggle> withSkillToggles;

        [SerializeField]
        private TMP_InputField _searchInputField;

        [SerializeField]
        private Button _deselectAllButton;

        [SerializeField]
        private Button _okButton;

        private ItemFilterOptions _itemFilterOptions;

#region Popup

        protected override void Awake()
        {
            base.Awake();

            // 등급 토글은 시트의 실제 등급으로 구성한다(등급 추가 시 코드/프리팹 변경 불필요).
            BuildGradeToggles();
            InitializeToggleGroup();

            // 등급 번호로 폴백한 칸은 L10N 키가 비어 있어서 셀 자신의 구독으로는
            // 언어 변경을 따라가지 못한다. 언어가 바뀌면 키부터 다시 평가한다
            // (그 사이 리모트 L10N 으로 키가 도착했을 수도 있다).
            L10nManager.OnLanguageChange
                .Subscribe(_ => RefreshGradeLabels())
                .AddTo(gameObject);

            CloseWidget = () =>
            {
                if (_searchInputField.isFocused)
                {
                    return;
                }

                Close(true);
            };

            _deselectAllButton.onClick.AddListener(DeselectAll);
            _okButton.onClick.AddListener(OnClickOkButton);
        }

#endregion Popup

        /// <summary>
        /// 시트의 실제 등급을 기준으로 등급 토글을 구성한다.
        /// 프리팹 토글을 풀로 쓰고, 등급이 더 많으면 복제하고 남으면 숨긴다.
        /// </summary>
        /// <remarks>
        /// 등급 목록의 권위가 시트에 있으므로 칸·라벨·색·레이아웃이 시트 행만으로 따라온다.
        /// 색은 배경 스프라이트와 같은 배색 규칙(9등급부터 1~8 색 재사용)을 따른다 —
        /// <c>LocalizationExtensions.GetItemGradeColor</c> 참고.
        /// </remarks>
        private void BuildGradeToggles()
        {
            if (gradeToggles is null || gradeToggles.Count == 0)
            {
                NcDebug.LogError(
                    $"{GetType().Name}: gradeToggles 가 비어 있습니다. 프리팹 바인딩을 확인하세요.");
                return;
            }

            var groups = BuildGradeGroups();
            if (groups.Count == 0)
            {
                // 시트가 아직 로드되지 않았다면 등급 필터를 구성할 수 없다. 비활성으로 두고
                // 나머지 필터는 정상 동작하게 한다(예외로 팝업 전체를 죽이지 않는다).
                NcDebug.LogWarning(
                    $"{GetType().Name}: 시트에서 등급을 찾지 못해 등급 필터를 비활성화합니다.");
                foreach (var t in gradeToggles)
                {
                    if (t?.toggle != null && !t.IsAll)
                    {
                        t.toggle.gameObject.SetActive(false);
                    }
                }

                return;
            }

            // "전체" 토글(option == 0)은 등급을 담지 않으므로 풀에서 제외한다.
            var pool = gradeToggles.FindAll(t => t is not null && t.toggle != null && !t.IsAll);
            if (pool.Count == 0)
            {
                NcDebug.LogError(
                    $"{GetType().Name}: 등급 토글이 하나도 없습니다. 프리팹 바인딩을 확인하세요.");
                return;
            }

            var bound = gradeToggles.FindAll(t => t is not null && t.toggle != null).Count;
            if (bound != gradeToggles.Count)
            {
                NcDebug.LogError(
                    $"{GetType().Name}: 등급 토글 {gradeToggles.Count - bound}개가 비어 있습니다." +
                    " 프리팹 바인딩을 확인하세요.");
            }

            // 프리팹에서 등급 셀을 늘리면 새 항목의 option 기본값이 0(= "전체")이라
            // "전체" 토글이 둘이 된다. 등급 셀은 이제 코드가 만들므로 그럴 일이 없지만,
            // 조용히 이상 동작하는 대신 알린다.
            if (bound - pool.Count > 1)
            {
                NcDebug.LogError(
                    $"{GetType().Name}: option == 0(\"전체\") 토글이 {bound - pool.Count}개입니다." +
                    " 등급 셀의 option 을 0 이 아닌 값으로 두세요.");
            }

            // 등급은 오름차순으로 배정하므로 풀도 화면 순서(= 계층 순서)로 고정한다.
            // 프리팹 리스트의 순서에 의존하면 바인딩을 재정렬하는 순간 라벨이 뒤바뀐다.
            pool.Sort((a, b) =>
                a.toggle.transform.GetSiblingIndex().CompareTo(b.toggle.transform.GetSiblingIndex()));

            // 부족하면 마지막 토글을 같은 부모 아래로 복제한다(레이아웃 유지).
            var template = pool[pool.Count - 1];
            while (pool.Count < groups.Count)
            {
                var clonedGo = Instantiate(template.toggle.gameObject, template.toggle.transform.parent);
                var clonedToggle = clonedGo.GetComponent<Toggle>();
                if (clonedToggle == null)
                {
                    Destroy(clonedGo);
                    break;
                }

                clonedToggle.isOn = false;
                var cloned = new GradeToggle
                {
                    toggle = clonedToggle,
                    // 0 은 "전체" 를 뜻하므로 쓰지 않는다. 값 자체는 이제 의미가 없다.
                    option = -1,
                    isCloned = true,
                };
                gradeToggles.Add(cloned);
                pool.Add(cloned);
            }

            // 등급 배정. 남는 토글은 숨긴다(등급이 줄어든 시트에도 안전하게 대응).
            for (var i = 0; i < pool.Count; i++)
            {
                var t = pool[i];
                if (i < groups.Count)
                {
                    t.grades = groups[i].grades;
                    t.labelKey = groups[i].labelKey;
                    t.toggle.gameObject.SetActive(true);
                }
                else
                {
                    t.grades = Array.Empty<int>();
                    t.labelKey = null;
                    t.toggle.gameObject.SetActive(false);
                }

                ApplyLabelKey(t);
                ApplyClonedLabelColor(t);
            }

            FitCells(pool, Math.Min(groups.Count, pool.Count));
        }

        /// <summary>
        /// 셀 라벨의 <c>L10nTextMeshProUGUI</c> 키를 배정된 등급에 맞춘다.
        /// </summary>
        /// <remarks>
        /// 이 컴포넌트는 언어가 바뀌면 자기 키로 텍스트를 다시 쓴다. 복제 셀은 템플릿의
        /// 키를 물려받으므로 그대로 두면 언어를 한 번 바꾼 순간 직전 등급 라벨로 돌아간다.
        /// 키가 아직 없는 등급이면 키를 비워, <see cref="GradeToggle.GetOptionName"/> 이
        /// 넣은 등급 번호가 언어 변경 후에도 유지되게 한다.
        /// 대상은 <see cref="BindToggleEvent{T}"/> 가 텍스트를 쓰는 그 라벨 하나뿐이다 —
        /// 셀에는 비활성 TMP 가 더 있고, 거기까지 건드리면 범위를 벗어난다.
        /// </remarks>
        private static void ApplyLabelKey(GradeToggle gradeToggle)
        {
            var label = gradeToggle.toggle.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                return;
            }

            var l10nText = label.GetComponent<L10nTextMeshProUGUI>();
            if (l10nText == null)
            {
                return;
            }

            var key = gradeToggle.labelKey;
            var localizable = !string.IsNullOrEmpty(key) && L10nManager.ContainsKey(key);
            l10nText.L10nKey = localizable ? key : null;
        }

        /// <summary>
        /// 복제 칸의 라벨 색을 담당 등급 색으로 맞춘다.
        /// </summary>
        /// <remarks>
        /// 복제본은 템플릿(최고 등급) 색을 물려받으므로 그대로 두면 새 등급 칸이 직전
        /// 등급과 같은 색이 된다. 대표색은 <b>그룹의 최고 등급</b>이다 — 프리팹의
        /// "Below Epic"({1,2}) 칸이 2등급 초록인 것과 같은 규칙이다.
        /// 프리팹 칸은 건드리지 않는다. 색이 팔레트와 거의 같지만 7등급 한 칸이 b 채널만
        /// 다르게(0.667 vs 0.529) 손수 조정돼 있어, 코드가 소유권을 가져가면 그 칸 색이
        /// 조용히 바뀐다. 팔레트를 유일한 권위로 삼는 건 디자이너 확인이 필요한 결정이다.
        /// 색 규칙 자체는 <c>LocalizationExtensions.GetItemGradeColor</c> 한 곳에만 둔다.
        /// </remarks>
        private static void ApplyClonedLabelColor(GradeToggle gradeToggle)
        {
            if (!gradeToggle.isCloned || gradeToggle.grades is not { Length: > 0 })
            {
                return;
            }

            var label = gradeToggle.toggle.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                return;
            }

            var grades = gradeToggle.grades;
            label.color = LocalizationExtensions.GetItemGradeColor(grades[grades.Length - 1]);
        }

        /// <summary>
        /// 컬럼 높이가 프리팹에 고정(350)이라 칸이 늘면 프레임 밖으로 삐져나온다.
        /// 칸 수에 맞춰 세로 스케일을 줄여 항상 안에 들어오게 한다.
        /// </summary>
        /// <remarks>
        /// <c>RectTransform</c> 높이가 아니라 <c>localScale.y</c> 를 줄인다. 컬럼의
        /// <c>VerticalLayoutGroup</c> 은 <c>childScaleHeight</c> 가 켜져 있어 칸 크기와
        /// 배치 간격을 모두 스케일에 곱하는데, 높이만 줄이면 칸 <b>안</b>의 배경·프레임은
        /// 세로 중앙 앵커에 고정 높이라 그대로 남아 서로 파고든다.
        /// 줄이는 방향으로만 손댄다 — 지금의 8칸(44 × 8 + 간격 −1 × 7 = 345)은 그대로다.
        /// 이게 없으면 등급이 하나 늘 때마다 프리팹 레이아웃을 손봐야 한다.
        /// 대가는 <b>세로로만</b> 눌리는 비등방 압축이다(9칸 0.90, 10칸 0.81). 라운드
        /// 코너와 글리프가 찌그러지므로, 칸이 더 늘면 컬럼 자체를 키우는 프리팹 작업이 맞다.
        /// 팝업이 이미 떠 있는 상태에서 부르게 되면
        /// <c>LayoutRebuilder.MarkLayoutForRebuild(parent)</c> 가 필요하다 — 지금은
        /// <c>Awake</c> 에서만 부르고 첫 <c>Show()</c> 의 <c>OnEnable</c> 이 리빌드를 보장한다.
        /// </remarks>
        private static void FitCells(List<GradeToggle> pool, int visibleCount)
        {
            if (visibleCount <= 0)
            {
                return;
            }

            if (pool[0].toggle.transform.parent is not RectTransform parent)
            {
                return;
            }

            // 레이아웃 그룹이 없거나 높이를 직접 정하는 설정이면 스케일로는 맞출 수 없다
            // (칸을 다시 배치할 주체가 없어 그래픽만 줄고 위치는 그대로다).
            var layout = parent.GetComponent<VerticalLayoutGroup>();
            if (layout == null || layout.childControlHeight || !layout.childScaleHeight)
            {
                NcDebug.LogWarning(
                    $"{nameof(ItemFilterPopupBase)}: 등급 컬럼의 레이아웃 설정으로는 칸 높이를" +
                    " 맞출 수 없습니다. 컬럼 높이를 직접 조정해야 합니다.");
                return;
            }

            var available = parent.rect.height
                - layout.padding.top
                - layout.padding.bottom;
            var spacing = layout.spacing;

            // 스케일 기준은 스케일이 적용되지 않은 rect 높이의 합이라 몇 번 불러도 결과가 같다.
            var total = 0f;
            for (var i = 0; i < visibleCount; i++)
            {
                if (pool[i].toggle.transform is RectTransform rect)
                {
                    total += rect.rect.height;
                }
            }

            if (total <= 0f)
            {
                // 레이아웃이 아직 확정되지 않았거나 컬럼이 접혀 있다. 건드리지 않는다.
                return;
            }

            var scale = (available - (spacing * (visibleCount - 1))) / total;
            if (scale <= 0f || scale >= 1f)
            {
                return;
            }

            for (var i = 0; i < visibleCount; i++)
            {
                var transform = pool[i].toggle.transform;
                var localScale = transform.localScale;
                transform.localScale = new Vector3(localScale.x, scale, localScale.z);
            }
        }

        /// <summary>
        /// 시트의 distinct 등급을 토글 단위로 묶고 라벨 키를 함께 정한다.
        /// 하위 등급(<see cref="BelowEpicMaxGrade"/> 이하)은 한 칸으로 합친다.
        /// </summary>
        private static List<(int[] grades, string labelKey)> BuildGradeGroups()
        {
            var groups = new List<(int[] grades, string labelKey)>();

            var itemSheet = Game.Game.instance?.TableSheets?.ItemSheet;
            if (itemSheet is null)
            {
                return groups;
            }

            var grades = new SortedSet<int>();
            foreach (var row in itemSheet.Values)
            {
                // grade 0 은 초기 지급 장비(Wooden Club / Ragged Clothes)뿐이라 등급 체계 밖이다.
                if (row.Grade > 0)
                {
                    grades.Add(row.Grade);
                }
            }

            var below = new List<int>();
            foreach (var g in grades)
            {
                if (g <= BelowEpicMaxGrade)
                {
                    below.Add(g);
                }
            }

            if (below.Count > 0)
            {
                // 여러 등급을 묶은 칸은 전용 키를, 하나뿐이면 그 등급 키를 쓴다.
                groups.Add((below.ToArray(),
                    below.Count > 1
                        ? BelowEpicGradeKey
                        : string.Format(GradeKeyFormat, below[0])));
            }

            foreach (var g in grades)
            {
                if (g <= BelowEpicMaxGrade)
                {
                    continue;
                }

                if (groups.Count >= MaxGradeToggleCount)
                {
                    NcDebug.LogWarning(
                        $"{nameof(ItemFilterPopupBase)}: 등급 칸 상한({MaxGradeToggleCount})을" +
                        $" 넘었습니다. 등급 {g} 이상을" +
                        " 필터에서 제외합니다. 시트의 grade 값을 확인하세요.");
                    break;
                }

                groups.Add((new[] { g }, string.Format(GradeKeyFormat, g)));
            }

            return groups;
        }

        private void InitializeToggleGroup()
        {
            BindToggleEvent(gradeToggles);
            BindToggleEvent(elementalToggles);
            BindToggleEvent(itemTypeToggles);
            BindToggleEvent(upgradeLevelToggles);
            BindToggleEvent(optionCountToggles);
            BindToggleEvent(withSkillToggles);
        }

        /// <summary>
        /// 토글 이름과 라벨 텍스트를 옵션 이름으로 맞춘다.
        /// </summary>
        /// <remarks>
        /// 일부 프리팹은 UGUI <c>Text</c> 대신 TMP 를 쓴다. 복제로 만든 등급 칸은 템플릿의
        /// 텍스트를 그대로 물려받으므로, 여기서 덮지 않으면 같은 라벨이 두 개로 보인다.
        /// </remarks>
        private static void ApplyOptionName(ItemToggleType item)
        {
            var optionName = item.GetOptionName;
            item.toggle.name = optionName;

            var uguiText = item.toggle.GetComponentInChildren<Text>(true);
            if (uguiText != null)
            {
                uguiText.text = optionName;
            }

            var tmpText = item.toggle.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
            {
                tmpText.text = optionName;
            }
        }

        /// <summary>
        /// 언어가 바뀐 뒤 등급 칸의 L10N 키와 라벨을 다시 적용한다.
        /// </summary>
        private void RefreshGradeLabels()
        {
            if (gradeToggles is null)
            {
                return;
            }

            foreach (var t in gradeToggles)
            {
                if (t is null || t.toggle == null)
                {
                    continue;
                }

                if (!t.IsAll)
                {
                    ApplyLabelKey(t);
                }

                ApplyOptionName(t);
            }
        }

        private void BindToggleEvent<T>(List<T> toggles) where T : ItemToggleType
        {
            if (toggles is null)
            {
                NcDebug.LogError($"{GetType().Name}: 토글 리스트가 null 입니다. 프리팹 바인딩을 확인하세요.");
                return;
            }

            foreach (var item in toggles)
            {
                if (item is null || item.toggle == null)
                {
                    NcDebug.LogError(
                        $"{GetType().Name}: 토글 바인딩이 비어 있습니다. 프리팹을 확인하세요.");
                    continue;
                }

                ApplyOptionName(item);

                if (item.IsAll)
                {
                    item.toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn)
                        {
                            ResetToAll(toggles);
                        }
                        else if (IsOffAllToggle(toggles))
                        {
                            ResetToAll(toggles);
                        }
                    });
                }
                else
                {
                    item.toggle.onValueChanged.AddListener(isOn =>
                    {
                        if (isOn)
                        {
                            OffAllToggle(toggles);
                        }
                        else if (IsOffAllToggle(toggles))
                        {
                            ResetToAll(toggles);
                        }
                    });
                }
            }

            ResetToAll(toggles);
        }

        private bool IsOffAllToggle<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
            {
                if (item.toggle.isOn)
                {
                    return false;
                }
            }

            return true;
        }

        private void OffAllToggle<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
            {
                item.OffAllToggle();
            }
        }

        private void ResetToAll<T>(List<T> toggles) where T : ItemToggleType
        {
            foreach (var item in toggles)
            {
                item.ResetToAll();
            }
        }

        private void DeselectAll()
        {
            ResetToAll(gradeToggles);
            ResetToAll(elementalToggles);
            ResetToAll(itemTypeToggles);
            ResetToAll(upgradeLevelToggles);
            ResetToAll(optionCountToggles);
            ResetToAll(withSkillToggles);
            _searchInputField.text = string.Empty;
        }

        public void OnClickOkButton()
        {
            ApplyItemFilterOptionFromToggle();
            Close(true);
        }

        /// <summary>
        /// 현재 선택된 아이템 탭에 따라 적용할 필터 옵션을 활성화/비활성화 시킨다.
        /// 현재 gradeToggles를 제외한 모든 필터 토글이 Equipment 탭에서만 활성화 된다.
        /// </summary>
        /// <param name="itemType">현재 활성화된 아이템 탭</param>
        public void SetItemTypeTap(Nekoyume.Model.Item.ItemType itemType)
        {
            foreach (var elementalToggle in elementalToggles)
            {
                elementalToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }

            foreach (var itemTypeToggle in itemTypeToggles)
            {
                itemTypeToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }

            foreach (var upgradeLevelToggle in upgradeLevelToggles)
            {
                upgradeLevelToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }

            foreach (var optionCountToggle in optionCountToggles)
            {
                optionCountToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }

            foreach (var withSkillToggle in withSkillToggles)
            {
                withSkillToggle.toggle.interactable = itemType == Nekoyume.Model.Item.ItemType.Equipment;
            }
        }

        protected void ApplyItemFilterOptionFromToggle()
        {
            var itemFilterOptionType = new ItemFilterOptions();

            foreach (var gradeToggle in gradeToggles)
            {
                if (!gradeToggle.toggle.isOn || gradeToggle.IsAll)
                {
                    continue;
                }

                itemFilterOptionType.Grades ??= new HashSet<int>();
                foreach (var grade in gradeToggle.grades)
                {
                    itemFilterOptionType.Grades.Add(grade);
                }
            }

            foreach (var elementalToggle in elementalToggles)
            {
                itemFilterOptionType.Elemental |= elementalToggle.toggle.isOn ? elementalToggle.elemental : Elemental.All;
            }

            foreach (var itemTypeToggle in itemTypeToggles)
            {
                itemFilterOptionType.ItemType |= itemTypeToggle.toggle.isOn ? itemTypeToggle.itemType : ItemType.All;
            }

            foreach (var upgradeLevelToggle in upgradeLevelToggles)
            {
                itemFilterOptionType.UpgradeLevel |= upgradeLevelToggle.toggle.isOn ? upgradeLevelToggle.upgradeLevel : UpgradeLevel.All;
            }

            foreach (var optionCountToggle in optionCountToggles)
            {
                itemFilterOptionType.OptionCount |= optionCountToggle.toggle.isOn ? optionCountToggle.optionCount : OptionCount.All;
            }

            foreach (var withSkillToggle in withSkillToggles)
            {
                itemFilterOptionType.WithSkill |= withSkillToggle.toggle.isOn ? withSkillToggle.withSkill : WithSkill.All;
            }

            itemFilterOptionType.SearchText = _searchInputField.text;

            _itemFilterOptions = itemFilterOptionType;
        }

        protected ItemFilterOptions GetItemFilterOptionType()
        {
            return _itemFilterOptions;
        }

        protected void ResetViewFromFilterOption()
        {
            SetTogglesFromFilterOption();
            SetInputFiledFromFilterOption();
        }

        private void SetTogglesFromFilterOption()
        {
            if (_itemFilterOptions.Grades is { Count: > 0 })
            {
                var selected = _itemFilterOptions.Grades;
                foreach (var gradeToggle in gradeToggles)
                {
                    // 묶음 토글은 담당 등급이 모두 선택돼 있을 때만 켠다.
                    gradeToggle.toggle.isOn =
                        !gradeToggle.IsAll &&
                        gradeToggle.grades.Length > 0 &&
                        Array.TrueForAll(gradeToggle.grades, selected.Contains);
                }
            }
            else
            {
                ResetToAll(gradeToggles);
            }

            if (_itemFilterOptions.Elemental != Elemental.All)
            {
                foreach (var elementalToggle in elementalToggles)
                {
                    elementalToggle.toggle.isOn = _itemFilterOptions.Elemental.HasFlag(elementalToggle.elemental);
                }
            }
            else
            {
                ResetToAll(elementalToggles);
            }

            if (_itemFilterOptions.ItemType != ItemType.All)
            {
                foreach (var itemTypeToggle in itemTypeToggles)
                {
                    itemTypeToggle.toggle.isOn = _itemFilterOptions.ItemType.HasFlag(itemTypeToggle.itemType);
                }
            }
            else
            {
                ResetToAll(itemTypeToggles);
            }

            if (_itemFilterOptions.UpgradeLevel != UpgradeLevel.All)
            {
                foreach (var upgradeLevelToggle in upgradeLevelToggles)
                {
                    upgradeLevelToggle.toggle.isOn = _itemFilterOptions.UpgradeLevel.HasFlag(upgradeLevelToggle.upgradeLevel);
                }
            }
            else
            {
                ResetToAll(upgradeLevelToggles);
            }

            if (_itemFilterOptions.OptionCount != OptionCount.All)
            {
                foreach (var optionCountToggle in optionCountToggles)
                {
                    optionCountToggle.toggle.isOn = _itemFilterOptions.OptionCount.HasFlag(optionCountToggle.optionCount);
                }
            }
            else
            {
                ResetToAll(optionCountToggles);
            }

            if (_itemFilterOptions.WithSkill != WithSkill.All)
            {
                foreach (var withSkillToggle in withSkillToggles)
                {
                    withSkillToggle.toggle.isOn = _itemFilterOptions.WithSkill.HasFlag(withSkillToggle.withSkill);
                }
            }
            else
            {
                ResetToAll(withSkillToggles);
            }
        }

        private void SetInputFiledFromFilterOption()
        {
            _searchInputField.text = _itemFilterOptions.SearchText;
        }

        public static ItemType ItemSubTypeToItemType(ItemSubType itemSubType)
        {
            switch (itemSubType)
            {
                case ItemSubType.Weapon:
                    return ItemType.Weapon;
                case ItemSubType.Armor:
                    return ItemType.Armor;
                case ItemSubType.Belt:
                    return ItemType.Belt;
                case ItemSubType.Necklace:
                    return ItemType.Necklace;
                case ItemSubType.Ring:
                    return ItemType.Ring;
                case ItemSubType.Aura:
                    return ItemType.Aura;
                case ItemSubType.Grimoire:
                    return ItemType.Grimoire;
                default:
                    return ItemType.All;
            }
        }

        public static WithSkill ItemSubTypeToWithSkill(bool skillContains)
        {
            return skillContains ? WithSkill.With : WithSkill.None;
        }
    }
}
