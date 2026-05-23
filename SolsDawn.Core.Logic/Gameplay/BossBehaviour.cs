using System;
using System.Collections.Generic;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay;

public class BossAI(BossBehaviourBuilder builder, BossBehaviourContext context) 
{
    private IReadOnlyList<IBossInstruction> _instructions = builder.Build();
    private int _actionIndex = 0;
    
    public void Update()
    {
        if (context.Boss.CurrentState == Boss.State.Pending)
        {
            var instruction = _instructions[_actionIndex];
            _actionIndex = instruction.Execute(context, _actionIndex);
            _actionIndex %= _instructions.Count;
        }
    }
}