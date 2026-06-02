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
            var shouldContinue = true;
            while (shouldContinue)
            {
                var instruction = _instructions[_actionIndex];
                shouldContinue = instruction 
                    is JumpInstruction 
                    or JumpIfFalseInstruction 
                    or InstantActionInstruction
                    or ForgetInstruction;
                
                _actionIndex = instruction.Execute(blackboard, _actionIndex);
                if (_actionIndex >= _instructions.Count)
                {
                    BossBehaviourBuilder.Reset();
                    _actionIndex = 0;
                }
            }
        }
    }
}