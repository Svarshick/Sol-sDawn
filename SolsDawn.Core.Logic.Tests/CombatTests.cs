using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Combat;
using SolsDawn.Gameplay.Entities;
using Xunit;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SolsDawn.Core.Logic.Tests;

[Collection("Library")]
public sealed class CombatTests : IDisposable
{
    private const float Dt = 0.1f;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "solsdawn-" + Guid.NewGuid().ToString("N"));

    public CombatTests() => Library.Initialize(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static SlideBlade NewBlade(float acceleration, float peak, float deceleration, float endSpeed)
    {
        var blade = new SlideBlade { AttackEndSpeed = endSpeed };
        CategorySystem.Bind(blade);

        var attack = Library.CreateCopyFromDefault(typeof(SimpleAttack));
        ((SimpleAttack)attack.Instance!).Acceleration = acceleration;
        ((SimpleAttack)attack.Instance!).PeakSpeed = peak;
        blade.Attack.ReferenceModel(attack.Guid);

        var decay = Library.CreateCopyFromDefault(typeof(LinearSlowdown));
        ((LinearSlowdown)decay.Instance!).Deceleration = deceleration;
        blade.Decay.ReferenceModel(decay.Guid);

        return blade;
    }

    private static List<(float Speed, SlideBladePhase Phase, bool Ended)> Run(SlideBlade blade, int maxFrames)
    {
        var frames = new List<(float, SlideBladePhase, bool)>();
        var speed = 0f;
        var phase = SlideBladePhase.Attack;
        for (var i = 0; i < maxFrames; i++)
        {
            var ended = blade.Step(ref speed, ref phase, Dt);
            frames.Add((speed, phase, ended));
            if (ended)
                break;
        }

        return frames;
    }

    [Fact]
    public void CMB32_AttackRisesToPeakThenDecayEndsAtEndSpeed()
    {
        var frames = Run(NewBlade(acceleration: 10f, peak: 15f, deceleration: 10f, endSpeed: 5f), 100);

        Assert.Equal(25, frames.Count);
        Assert.Equal(1f, frames[0].Speed, 4);
        Assert.Equal(15f, frames[14].Speed, 4);
        Assert.Equal(SlideBladePhase.Decay, frames[14].Phase);
        Assert.All(frames.Take(24), f => Assert.False(f.Ended));
        Assert.True(frames[^1].Ended);
        Assert.Equal(5f, frames[^1].Speed, 4);
    }

    [Fact]
    public void CMB32_PeakBelowEndSpeedEndsOnFirstDecayFrame()
    {
        var frames = Run(NewBlade(acceleration: 10f, peak: 3f, deceleration: 10f, endSpeed: 5f), 100);

        Assert.Equal(6, frames.Count);
        Assert.Equal(SlideBladePhase.Decay, frames[4].Phase);
        Assert.True(frames[5].Ended);
        Assert.Equal(5f, frames[5].Speed, 4);
    }

    [Fact]
    public void CMB32_SwappedDecayModelTakesEffectAtOnce()
    {
        var blade = NewBlade(acceleration: 100f, peak: 10f, deceleration: 1f, endSpeed: 0f);
        var speed = 0f;
        var phase = SlideBladePhase.Attack;
        blade.Step(ref speed, ref phase, Dt);

        var faster = Library.CreateCopyFromDefault(typeof(LinearSlowdown));
        ((LinearSlowdown)faster.Instance!).Deceleration = 50f;
        blade.Decay.ReferenceModel(faster.Guid);
        blade.Step(ref speed, ref phase, Dt);

        Assert.Equal(5f, speed, 4);
    }

    [Theory]
    [InlineData(10f, 4f, 20f, 5f)]
    [InlineData(-4f, 0f, 20f, 0f)]
    [InlineData(100f, 0f, 20f, 20f)]
    public void CMB43_ParryPointIsHalfTheProjectionOnTheAxis(float boxX, float boxY, float distance, float expected)
    {
        var t = CombatMath.FireParryDistance(Vector2.Zero, Vector2.UnitX, [new Vector2(boxX, boxY)], distance);

        Assert.Equal(expected, t, 4);
    }

    [Fact]
    public void CMB43_NearestParryboxGivesThePoint()
    {
        var t = CombatMath.FireParryDistance(Vector2.Zero, Vector2.UnitX, [new Vector2(10, 0), new Vector2(6, 1)], 20f);

        Assert.Equal(3f, t, 4);
    }
}
