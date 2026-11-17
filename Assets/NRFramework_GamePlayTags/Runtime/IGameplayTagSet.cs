namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 身上有没有某一类标签。集合容器和计数容器都用这一句来回答。
    /// </summary>
    public interface IGameplayTagSet
    {
        bool HasTag(GameplayTag tag);
    }
}
