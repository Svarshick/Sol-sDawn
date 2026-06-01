using System.Collections.Generic;
using SolsDawn.Core.Logic.Configs.Utils;
using SolsDawn.Core.Logic.Gameplay.Behaviour.Actors;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public class BossController(FightBlackboard blackboard, BossBehaviourBuilder builder) 
{
    private IReadOnlyList<IBossInstruction> _instructions = builder.Build();
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