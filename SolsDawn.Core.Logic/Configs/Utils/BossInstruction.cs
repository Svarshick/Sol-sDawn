namespace SolsDawn.Core.Logic.Configs.Utils;

public delegate void FightAction(FightBlackboard context);

public delegate bool FightActionCondition(FightBlackboard context);


public interface IInstruction
{
    public int Execute(FightBlackboard context, int currentIndex);
}

public class InstantActionInstruction(FightAction action) : IInstruction
{
    public int Execute(FightBlackboard context, int currentIndex)
    {
        action(context);
        return currentIndex + 1;
    }   
}

public class ActionInstruction(FightAction action) : IInstruction
{
    public int Execute(FightBlackboard context, int currentIndex)
    {
        action(context);
        return currentIndex + 1;
    }
}

public class JumpInstruction : IInstruction
{
    public int Destination = -1;
    public int Execute(FightBlackboard context, int currentIndex) => Destination;
}

public class JumpIfFalseInstruction(FightActionCondition condition) : IInstruction
{
    public int Destination = -1;

    public int Execute(FightBlackboard context, int currentIndex)
    {
        return condition(context) ? currentIndex + 1 : Destination;
    }
}

public class ForgetInstruction : IInstruction
{
    public int Execute(FightBlackboard context, int currentIndex)
    {
        context.Forget();
        return currentIndex + 1;
    }
}