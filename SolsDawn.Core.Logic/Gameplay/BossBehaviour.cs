using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Configs.Utils;

namespace SolsDawn.Core.Logic.Gameplay;

public class BossAI(BossBehaviourBuilder builder, BossBehaviourContext context)
{
    private IReadOnlyList<Action<BossBehaviourContext>> _actions = builder.Build();
    private int _actionIndex = 0;
    
    public void Update(GameTime gameTime)
    {
        if (context.Boss.CurrentState == Boss.State.Pending)
        {
            _actions[_actionIndex](context);
            _actionIndex = (_actionIndex + 1) % _actions.Count;
        }
    }
}