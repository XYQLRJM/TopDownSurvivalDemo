/// <summary>
/// 玩家不同攻击方式的策略接口。
/// </summary>
public interface IPlayerAttackStrategy
{
    /// <summary>尝试执行一次攻击，成功攻击时返回 true。</summary>
    bool TryAttack(PlayerAttackContext context);
}
