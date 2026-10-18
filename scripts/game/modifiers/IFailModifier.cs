/// <summary>
/// Modifiers that override the fail condition
/// </summary>
public interface IFailModifier : IModifier
{
    bool IsFail { get; }

    bool CheckFailCondition(bool hit, double health);
}
