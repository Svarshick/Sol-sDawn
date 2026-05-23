using System;
using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic;

public class GameTests : IUpdatable, IDrawable
{
    public bool IsActive = false;
    private StarBlinkAnimation _starBlink;

    public GameTests()
    {
        if (!IsActive)
            return;
        
    }

    private double _lastTime;
    public void Update()
    {
         if (!IsActive)
            return;

         if (Time.TotalGameTime.TotalSeconds - _lastTime > 2)
         {
             _lastTime = Time.TotalGameTime.TotalSeconds;
             var trans = new Transform();
             trans.Position = new Vector2(-100, -100);
             _starBlink = new StarBlinkAnimation(
                 true,
                 0.3f,
                 trans,
                 1,
                 (float)Math.PI,
                 20,
                 200,
                 Color.Yellow,
                 Color.White,
                 5);
         }
    }

    public void LateUpdate()
    {
        if (!IsActive)
            return;
    }

    public void Draw()
    {
        if (!IsActive)
            return;
        
        if (_starBlink is not null && !_starBlink.IsFinished)
            _starBlink.Draw();
    }
}