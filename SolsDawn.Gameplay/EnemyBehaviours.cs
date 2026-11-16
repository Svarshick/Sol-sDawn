namespace SolsDawn.Gameplay;

public static class EnemyBehaviours
{
    public static async Job Duelist(Enemy enemy)
    {
        while (true)
        {
            await BladeAttack(enemy);
            await Timer(enemy.Board.Specs.GetActiveInstance().AttackGap);
            await FireAttack(enemy);
            await Timer(enemy.Board.Specs.GetActiveInstance().AttackGap);
        }
    }

    private static Vector2 AimAtPlayer(Enemy enemy) => CombatMath.Direction(enemy.Transform.Position, G.Player?.Transform.Position ?? enemy.Transform.Position);

    private static async Job BladeAttack(Enemy enemy)
    {
        var s = enemy.Board.Specs.GetActiveInstance();
        var position = enemy.Transform.Position;
        var aim = AimAtPlayer(enemy);
        var bladeCenter = position + aim * s.BladeLength / 2;
        var bladeShape = Shapes.Rectangle(s.BladeLength, s.BladeWidth);

        var parrybox = new Parrybox(CreateObject(bladeCenter, aim.Angle()), bladeShape, Side.Enemy, ParryKind.Blade);
        enemy.Board.Tint = s.BladeTelegraphColor;
        await Race(Timer(s.BladeTelegraphDuration), parrybox.Parried).Finished;
        var parried = parrybox.Parried.IsFired;
        parrybox.Destroy();

        if (parried)
        {
            enemy.Board.Tint = s.BladeParriedColor;
            await Timer(s.BladeParriedDuration);
            enemy.Board.Tint = null;
            return;
        }

        enemy.Board.Tint = null;
        var attack = new Attack(CreateObject(bladeCenter, aim.Angle()), bladeShape, Side.Player, ParryKind.None);
        Game.AnimationsPool.Add(new LineTraceAnimation(position, position + aim * s.BladeLength, s.BladeWidth, s.BladeAttackDuration, s.BladeAttackColor));
        await Timer(s.BladeAttackDuration);
        attack.Destroy();
    }

    private static async Job FireAttack(Enemy enemy)
    {
        var s = enemy.Board.Specs.GetActiveInstance();
        var position = enemy.Transform.Position;
        var aim = AimAtPlayer(enemy);

        var parrybox = new Parrybox(CreateObject(position), Enemy.BodyShape(s), Side.Enemy, ParryKind.Fire);
        enemy.Board.Tint = s.FireTelegraphColor;
        var telegraph = Timer(s.FireTelegraphDuration);
        var elapsed = 0f;
        while (!telegraph.IsEnded && !parrybox.Parried.IsFired)
        {
            if (elapsed < s.FireAimingDuration)
                aim = AimAtPlayer(enemy);
            elapsed += DeltaTime;
            await NextFrame();
        }

        var parried = parrybox.Parried.IsFired;
        var parriedBy = parrybox.ParriedBy;
        parrybox.Destroy();

        if (parried)
        {
            var target = parriedBy?.ParryPoint ?? (position + (G.Player?.Transform.Position ?? position)) / 2;
            Game.AnimationsPool.Add(new LineTraceAnimation(position, target, s.FireWidth, s.FireTraceDuration, s.FireTraceColor));
            enemy.Board.Tint = s.FireParriedColor;
            await Timer(s.FireParriedDuration);
            enemy.Board.Tint = null;
            return;
        }

        enemy.Board.Tint = null;
        var attack = new Attack(
            CreateObject(position + aim * s.FireDistance / 2, aim.Angle()),
            Shapes.Rectangle(s.FireDistance, s.FireWidth),
            Side.Player,
            ParryKind.None);
        await attack.Resolved;
        Game.AnimationsPool.Add(new LineTraceAnimation(position, position + aim * s.FireDistance, s.FireWidth, s.FireTraceDuration, s.FireTraceColor));
        attack.Destroy();
    }
}
