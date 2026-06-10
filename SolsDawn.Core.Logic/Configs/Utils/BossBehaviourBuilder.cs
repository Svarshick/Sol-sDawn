using System;
using System.Collections.Generic;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Configs.Utils;

public record FightBlackboard(Boss Boss, Player Player, ScreenLayout Layout)
{
    public bool IsBossLastBladeSuccess;
    public bool IsBossLastBladeParried { get; set; }
    public bool IsBossLastFireSuccess;
    public bool IsBossLastFireParried;
    
    public bool IsPlayerLastBladeSuccess;
    public bool IsPlayerLastFireSuccess;

    public void Forget()
    {
        IsBossLastBladeSuccess = false;
        IsBossLastBladeParried = false;
        IsBossLastFireSuccess = false;
        IsBossLastFireParried = false;

        IsPlayerLastBladeSuccess = false;
        IsPlayerLastFireSuccess = false;
    }
}

//LEGACY
/*public class BossBehaviourBuilder
{
    //DIRTY DIRTY DIRTY
    public static event Action RESET;
    public static void Reset() => RESET.Invoke();
    
    private readonly List<IInstruction> _instructions = new();
    private readonly Stack<IfGroup> _ifStack = new();
    private readonly Stack<WhileScope> _whileStack = new();

    private class IfGroup
    {
        public JumpIfFalseInstruction CurrentBranch;
        public readonly List<JumpInstruction> BranchExits = new();
    }

    private class WhileScope(JumpIfFalseInstruction exitCondition, int exitConditionAddress)
    {
        public readonly int ExitConditionAddress = exitConditionAddress;
        public readonly JumpIfFalseInstruction ExitCondition = exitCondition;
        public readonly List<JumpInstruction> Breaks = new();
    }

    private BossBehaviourBuilder()
    {
    }

    public static BossBehaviourBuilder Create() => new();

    public IReadOnlyList<IInstruction> Build()
    {
        if (_ifStack.Count > 0)
            throw new InvalidOperationException("Unclosed If block detected. Make sure to call EndIf.");

        return _instructions;
    }

    public BossBehaviourBuilder Then(Func<BossBehaviourBuilder> nextFactory)
    {
        _instructions.AddRange(nextFactory()._instructions);
        return this;
    }

    public BossBehaviourBuilder Wait(float seconds)
    {
        _instructions.Add(new ActionInstruction(ctx =>
        {
            var state = new Boss.IdleState(ctx.Boss, seconds);
            IntentionsPool.Add(State.Intend(ctx.Boss.GameObject, state));
        }));
        return this;
    }

    public BossBehaviourBuilder Teleport(VectorExpression position)
    {
        _instructions.Add(new ActionInstruction(ctx =>
        {
            var state = new Boss.TeleportState(ctx.Boss, position.Evaluate(ctx));
            IntentionsPool.Add(State.Intend(ctx.Boss.GameObject, state));
        }));
        return this;
    }

    public BossBehaviourBuilder Blade(VectorExpression lookPosition)
    {
        _instructions.Add(new ActionInstruction(ctx =>
        {
            var state = new Boss.BladeTelegraphState(ctx.Boss, ctx, lookPosition.Evaluate(ctx));
            IntentionsPool.Add(State.Intend(ctx.Boss.GameObject, state));
        }));
        return this;
    }

    public BossBehaviourBuilder Fire(VectorExpression lookPosition)
    {
        _instructions.Add(new ActionInstruction(ctx =>
        {
            var state = new Boss.FireTelegraphState(ctx.Boss, ctx, () => lookPosition.Evaluate(ctx));
            IntentionsPool.Add(State.Intend(ctx.Boss.GameObject, state));
        }));
        return this;
    }

    public BossBehaviourBuilder SpawnOrb(VectorExpression position, VectorExpression target, OrbStats stats)
    {
        _instructions.Add(new InstantActionInstruction(ctx =>
        {
            ctx.OrbController.Spawn(position.Evaluate(ctx), () => target.Evaluate(ctx), stats);
        }));
        return this;
    }

    public BossBehaviourBuilder If(BoolExpression condition)
    {
        var branch = new JumpIfFalseInstruction(condition);
        _instructions.Add(branch);
        var ifGroup = new IfGroup { CurrentBranch = branch };
        _ifStack.Push(ifGroup);
        return this;
    }

    public BossBehaviourBuilder ElseIf(BoolExpression condition)
    {
        if (_ifStack.Count == 0)
        {
            throw new InvalidOperationException("ElseIf must be called inside an If block.");
        }

        var exit = new JumpInstruction();
        _instructions.Add(exit);
        var branch = new JumpIfFalseInstruction(condition);
        var ifGroup = _ifStack.Peek();
        ifGroup.CurrentBranch.Destination = _instructions.Count;
        ifGroup.CurrentBranch = branch;
        ifGroup.BranchExits.Add(exit);
        _instructions.Add(branch);
        return this;
    }

    public BossBehaviourBuilder Else()
    {
        if (_ifStack.Count == 0)
        {
            throw new InvalidOperationException("Else must be called inside an If block.");
        }

        var exit = new JumpInstruction();
        _instructions.Add(exit);
        var ifGroup = _ifStack.Peek();
        ifGroup.CurrentBranch.Destination = _instructions.Count;
        ifGroup.CurrentBranch = null;
        ifGroup.BranchExits.Add(exit);
        return this;
    }

    public BossBehaviourBuilder EndIf()
    {
        if (_ifStack.Count == 0)
        {
            throw new InvalidOperationException("EndIf outside If block.");
        }

        var ifGroup = _ifStack.Pop();
        if (ifGroup.CurrentBranch is not null)
        {
            ifGroup.CurrentBranch.Destination = _instructions.Count;
        }

        foreach (var branchExit in ifGroup.BranchExits)
        {
            branchExit.Destination = _instructions.Count;
        }

        return this;
    }

    public BossBehaviourBuilder While(BoolExpression condition)
    {
        var conditionInstruction = new JumpIfFalseInstruction(condition);
        var scope = new WhileScope(conditionInstruction, _instructions.Count);
        _instructions.Add(conditionInstruction);
        _whileStack.Push(scope);
        return this;
    }

    public BossBehaviourBuilder Continue()
    {
        if (_whileStack.Count == 0)
        {
            throw new InvalidOperationException("Continue must be called inside an While block.");
        }
        
        var continueInstruction = new JumpInstruction();
        var scope = _whileStack.Peek();
        continueInstruction.Destination = scope.ExitConditionAddress;
        _instructions.Add(continueInstruction);
        return this;
    }

    public BossBehaviourBuilder Break()
    {
        if (_whileStack.Count == 0)
        {
            throw new InvalidOperationException("Break must be called inside an While block.");
        }

        var breakInstruction = new JumpInstruction();
        var scope = _whileStack.Peek();
        scope.Breaks.Add(breakInstruction);
        _instructions.Add(breakInstruction);
        return this;
    }

    public BossBehaviourBuilder EndWhile()
    {
        if (_whileStack.Count == 0)
        {
            throw new InvalidOperationException("EndWhile outside While block.");
        }

        var scope = _whileStack.Pop();
        var jump = new JumpInstruction();
        jump.Destination = scope.ExitConditionAddress;
        _instructions.Add(jump);
        scope.ExitCondition.Destination = _instructions.Count;

        foreach (var loopBreak in scope.Breaks)
        {
            loopBreak.Destination = _instructions.Count;
        }

        return this;
    }

    public BossBehaviourBuilder Forget()
    {
        _instructions.Add(new ForgetInstruction());
        return this;
    }

    public static VectorExpression Rotate(VectorExpression target, float radians) => new RotateVectorExpression(target, radians);
    public static VectorExpression Normalize(VectorExpression target) => new NormalizeVectorExpression(target);
    public static VectorExpression Units(float x, float y) => new UnitsVectorExpression(x, y);
    public static VectorExpression Player => new PlayerPositionExpression();
    public static VectorExpression PlayerSnapshot = new PlayerPositionSnapshotExpression();
    public static VectorExpression Boss => new BossPositionExpression();
    public static VectorExpression BossSnapshot = new BossPositionSnapshotExpression();
    public static VectorExpression CameraCenter => new CameraCenterPositionExpression();
    public static VectorExpression CameraTopLeft => new CameraTopLeftPositionExpression();
    public static VectorExpression CameraTopRight => new CameraTopRightPositionExpression();
    public static VectorExpression CameraBottomLeft => new CameraBottomLeftPositionExpression();
    public static VectorExpression CameraBottomRight => new CameraBottomRightPositionExpression();
    
    public static FloatExpression Units(float units) => new UnitsFloatExpression(units);

    public static BoolExpression Count(int times) => new CountExpression(times);
    public static BoolExpression IsBossLastBladeSuccess => new IsBossLastBladeSuccessExpression();
    public static BoolExpression IsBossLastBladeParried => new IsBossLastBladeParriedExpression();
    public static BoolExpression IsBossLastFireSuccess => new IsBossLastFireSuccessExpression();
    public static BoolExpression IsBossLastFireParried => new IsBossLastFireParriedExpression();
    public static BoolExpression IsPlayerLastBladeSuccess => new IsPlayerLastBladeSuccessExpression();
    public static BoolExpression IsPlayerLastFireSuccess => new IsPlayerLastFireSuccessExpression();
}*/