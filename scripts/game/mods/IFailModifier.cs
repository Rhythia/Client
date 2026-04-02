
/// <summary>
/// Modifiers that override the fail condition
/// </summary>
public interface IFailModifier : IMod
{
	bool IsFail { get; }

	bool CheckFailCondition(bool hit, double health);
}
