using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic.Gameplay;

public class BossAI(Boss boss)
{
    private int i = 0;
    public void Update(GameTime gameTime)
    {
        if (boss.CurrentState == Boss.State.Pending)
        {
            switch (i)
            {
                case 0:
                    boss.Wait(1);
                    break;
                case 1:
                    boss.Teleport(new Vector2(400, 200));
                    break;
                case 2:
                    boss.Blade(new Vector2(0, 0));
                    break;
                case 3: 
                    boss.Wait(1);
                    break;
            }

            i++;
            i = i % 4;
        }
    }
}