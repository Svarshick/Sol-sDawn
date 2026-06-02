using System.Collections.Generic;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

public class BossController(FightBlackboard blackboard, BossBehaviourBuilder builder) 
{
    private IReadOnlyList<IInstruction> _instructions = builder.Build();
    private int _actionIndex = 0;
    
    public void Update()
    {
        if (blackboard.Boss.State is Boss.PendingState)
        {
            var instruction = _instructions[_actionIndex];
            _actionIndex = instruction.Execute(blackboard, _actionIndex);
            _actionIndex %= _instructions.Count;
        }
    }
}