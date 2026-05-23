namespace SolsDawn.Core.Logic.Configs.Utils;

public delegate void BossAction(BossBehaviourContext context);

public delegate bool BossActionCondition(BossBehaviourContext context);


public interface IBossInstruction
{
    public int Execute(BossBehaviourContext context, int currentIndex);
}

public class ActionInstruction(BossAction action) : IBossInstruction
{
    public int Execute(BossBehaviourContext context, int currentIndex)
    {
        action(context);
        return currentIndex + 1;
    }
}

public class JumpInstruction : IBossInstruction
{
    public int Destination = -1;
    public int Execute(BossBehaviourContext context, int currentIndex) => Destination;
}

public class ConditionalJumpInstruction(BossActionCondition condition) : IBossInstruction
{
    public int Destination = -1;

    public int Execute(BossBehaviourContext context, int currentIndex)
    {
        return condition(context) ? currentIndex + 1 : Destination;
    }
}

public class ForgetInstruction : IBossInstruction
{
    public int Execute(BossBehaviourContext context, int currentIndex)
    {
        context.Forget();
        return currentIndex + 1;
    }
}