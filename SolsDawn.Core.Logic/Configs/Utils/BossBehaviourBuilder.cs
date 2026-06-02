using System;
using System.Collections.Generic;
using SolsDawn.Core.Logic.Gameplay.Behaviour;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Configs.Utils;

public record struct FightBlackboard(Boss Boss, Player Player, OrbController OrbController, ScreenLayout Layout)
{
    public bool BossLastAttackSucceeded;

    public void Forget()
    {
        BossLastAttackSucceeded = false;
    }
}

public class BossBehaviourBuilder
{
    private readonly List<IInstruction> _instructions = new();
    private readonly Stack<IfGroup> _ifStack = new();
    private class IfGroup
    {
        public ConditionalJumpInstruction CurrentBranch;
        public readonly List<JumpInstruction> BranchExits = new();
    }
        
    
    private BossBehaviourBuilder() { }
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
            var state = new Boss.FireTelegraphState(ctx.Boss, ctx, lookPosition.Evaluate(ctx));
            IntentionsPool.Add(State.Intend(ctx.Boss.GameObject, state));
        }));
        return this;
    }

    public BossBehaviourBuilder SpawnOrb(VectorExpression position, VectorExpression target, OrbStats stats)
    {
        _instructions.Add(new ActionInstruction(ctx =>
        {
            ctx.OrbController.Spawn(position.Evaluate(ctx), () => target.Evaluate(ctx), stats);
        }));
        return this;
    }

    public BossBehaviourBuilder If(BoolExpression condition, bool forget = true)
    {
        var branch = new ConditionalJumpInstruction(condition);
        _instructions.Add(branch);
        if (forget)
        {
            _instructions.Add(new ForgetInstruction());
        }
        var ifGroup = new IfGroup { CurrentBranch = branch };
        _ifStack.Push(ifGroup);
        return this;
    }

    public BossBehaviourBuilder ElseIf(BoolExpression condition, bool forget = true)
    {
        if (_ifStack.Count == 0)
        {
            throw new InvalidOperationException("ElseIf must be called inside an If block.");
        }
        var exit = new JumpInstruction();
        _instructions.Add(exit);
        var branch = new ConditionalJumpInstruction(condition);
        var ifGroup = _ifStack.Peek();
        ifGroup.CurrentBranch.Destination = _instructions.Count;
        ifGroup.CurrentBranch = branch;
        ifGroup.BranchExits.Add(exit);
        _instructions.Add(branch);
        if (forget)
        {
            _instructions.Add(new ForgetInstruction());
        }
        return this;
    }

    public BossBehaviourBuilder Else(bool forget = true)
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
        if (forget)
        {
            _instructions.Add(new ForgetInstruction());
        }
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

    public static VectorExpression Units(float x, float y) => new UnitsVectorExpression(x, y);
    public static VectorExpression Player => new PlayerPositionExpression();
    public static VectorExpression PlayerSnapshot = new PlayerPositionSnapshotExpression();
    public static VectorExpression Boss => new BossPositionExpression();
    public static VectorExpression CameraCenter => new CameraCenterPositionExpression();
    public static VectorExpression CameraTopLeft => new CameraTopLeftPositionExpression();
    public static VectorExpression CameraTopRight => new CameraTopRightPositionExpression();
    public static VectorExpression CameraBottomLeft => new CameraBottomLeftPositionExpression();
    public static VectorExpression CameraBottomRight => new CameraBottomRightPositionExpression();
    public static FloatExpression Units(float units) => new UnitsFloatExpression(units);
    public static BoolExpression LastAttack => new LastAttackSucceededExpression();
}