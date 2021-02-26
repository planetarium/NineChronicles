namespace Nekoyume.State.Modifiers
{
    /// <summary>
    /// 기본적인 상태 변경자입니다.
    /// </summary>
    /// <typeparam name="T">변경할 상태의 타입을 설정합니다.</typeparam>
    public interface IStateModifier<T> where T : Model.State.State
    {
        /// <summary>
        /// 상태 변경자의 상태가 바뀌었는지 확인합니다.
        /// true: `LocalStateSettings`에서 감지해서 새롭게 저장한 후에 `false`를 할당합니다.
        /// false: Do nothing.
        /// </summary>
        bool dirty { get; set; }
        /// <summary>
        /// 상태 변경자가 비어 있는지 확인합니다.
        /// </summary>
        bool IsEmpty { get; }
        /// <summary>
        /// `state`를 변경합니다.
        /// </summary>
        /// <param name="state">변경할 상태</param>
        /// <returns></returns>
        T Modify(T state);
    }

    /// <summary>
    /// 추후에 휘발성과 비휘발성 변경자 이외에 수명을 갖는 비휘발성 변경자를 추가할 때 사용할 인터페이스 입니다.
    /// `BlockIndex`에 할당된 블록 인덱스 까지 비휘발 성질을 유지하는 방법 등으로 사용될 수 있습니다.
    /// </summary>
    public interface IHasExpiredBlockIndex
    {
        /// <summary>
        /// 블록 인덱스입니다.
        /// </summary>
        long BlockIndex { get; }
    }

    /// <summary>
    /// 누적형 상태 변경자입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAccumulatableStateModifier<T> : IStateModifier<T> where T : Model.State.State
    {
        /// <summary>
        /// 상태 변경자를 더합니다.
        /// </summary>
        /// <param name="modifier"></param>
        void Add(IAccumulatableStateModifier<T> modifier);
        /// <summary>
        /// 상태 변경자를 제거합니다.
        /// </summary>
        /// <param name="modifier"></param>
        void Remove(IAccumulatableStateModifier<T> modifier);
    }
}
