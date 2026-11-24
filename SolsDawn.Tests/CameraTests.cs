using Xunit;

namespace SolsDawn.Tests;

[Collection("Library")]
public sealed class CameraTests : IDisposable
{
    private const float Dt = 0.1f;
    private const float Extent = 100f;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "solsdawn-" + Guid.NewGuid().ToString("N"));

    public CameraTests() => Library.Initialize(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static void Put<TModel>(CategorySlot<IAdsrModel> slot, Action<TModel> setup) where TModel : class, IAdsrModel
    {
        var entry = Library.CreateCopyFromDefault(typeof(TModel));
        setup((TModel)entry.Instance!);
        slot.ReferenceModel(entry.Guid);
    }

    private static void PutAds(CategorySlot<IMovementModel> slot, float peak, float acceleration = 1000f, float deceleration = 1000f)
    {
        var entry = Library.CreateCopyFromDefault(typeof(Ads));
        var ads = (Ads)entry.Instance!;
        ads.PeakSpeed = peak;
        Put<TestAttack>(ads.Attack, m => m.Acceleration = acceleration);
        Put<TestSlowdown>(ads.Decay, m => m.Deceleration = deceleration);
        var draEntry = Library.CreateCopyFromDefault(typeof(Dra));
        var dra = (Dra)draEntry.Instance!;
        Put<TestSlowdown>(dra.Decay, m => m.Deceleration = deceleration);
        Put<TestSlowdown>(dra.Release, m => m.Deceleration = deceleration);
        Put<TestAttack>(dra.Attack, m => m.Acceleration = acceleration);
        ads.Turn.ReferenceModel(draEntry.Guid);
        slot.ReferenceModel(entry.Guid);
    }

    private static Follow NewFollow(float trigger, float distance, float peak, float deceleration = 1000f)
    {
        var follow = new Follow { Trigger = trigger, Distance = distance };
        CategorySystem.Bind(follow);
        PutAds(follow.Movement, peak, deceleration: deceleration);
        return follow;
    }

    private static Forward NewForward(float trigger, float distance, float reach, float lead)
    {
        var forward = new Forward { Trigger = trigger, Distance = distance };
        CategorySystem.Bind(forward);
        PutAds(forward.Reach, reach);
        PutAds(forward.Lead, lead);
        return forward;
    }

    private static float Step(CameraAxis model, ref CameraAxisState state, float c, ref float p, float dp)
    {
        p += dp;
        return CameraController.Step(model, ref state, c, p, dp, Extent, Dt);
    }

    [Fact]
    public void CAM03_TriggerStaysAtLeastDistance()
    {
        IConstrainedModel follow = new Follow { Trigger = 10f, Distance = 20f };
        follow.Constrain(nameof(CameraAxis.Trigger));
        Assert.Equal(10f, ((Follow)follow).Distance);

        var edited = new Follow { Trigger = 10f, Distance = 20f };
        ((IConstrainedModel)edited).Constrain(nameof(CameraAxis.Distance));
        Assert.Equal(20f, edited.Trigger);
    }

    [Fact]
    public void CAM23_NoTriggerWithoutOutwardMotion()
    {
        var follow = NewFollow(20f, 5f, 50f);
        var state = new CameraAxisState();
        var p = 30f;

        var c = Step(follow, ref state, 0f, ref p, 0f);
        Assert.False(state.Tracking);
        Assert.Equal(0f, c);

        c = Step(follow, ref state, c, ref p, -1f);
        Assert.False(state.Tracking);
    }

    [Fact]
    public void CAM25_FollowApproachesHoldsAndStopsWithThePlayer()
    {
        var follow = NewFollow(20f, 5f, 50f);
        var state = new CameraAxisState();
        var p = 19f;
        var c = 0f;

        c = Step(follow, ref state, c, ref p, 1f);
        Assert.True(state.Tracking);
        Assert.Equal(1, state.Side);
        Assert.Equal(5f, c, 3);

        for (var i = 0; i < 3; i++)
            c = Step(follow, ref state, c, ref p, 1f);
        Assert.Equal(p - 5f, c, 3);
        Assert.Equal(1, state.Stage);
        Assert.Equal(0f, state.Velocity);

        for (var i = 0; i < 5; i++)
            c = Step(follow, ref state, c, ref p, 1f);
        Assert.Equal(p - 5f, c, 3);

        var stopped = c;
        c = Step(follow, ref state, c, ref p, 0f);
        Assert.False(state.Tracking);
        Assert.Equal(stopped, c);
    }

    [Fact]
    public void CAM27_PlayerReversingDuringApproachEndsInWorldCoordinates()
    {
        var follow = NewFollow(20f, 5f, 20f);
        var state = new CameraAxisState();
        var p = 19f;
        var c = 0f;

        c = Step(follow, ref state, c, ref p, 1f);
        Assert.Equal(2f, c, 3);

        c = Step(follow, ref state, c, ref p, -5f);
        Assert.True(state.Tracking);
        Assert.False(state.Carry);
        Assert.Equal(4f, c, 3);

        c = Step(follow, ref state, c, ref p, -5f);
        Assert.False(state.Tracking);
        Assert.Equal(5f, c, 3);

        c = Step(follow, ref state, c, ref p, -5f);
        Assert.Equal(5f, c, 3);
    }

    [Fact]
    public void CAM23_TriggerOnTheOtherSideKeepsVelocity()
    {
        var follow = NewFollow(20f, 5f, 20f, deceleration: 10f);
        var state = new CameraAxisState();
        var p = 19f;
        var c = 0f;

        c = Step(follow, ref state, c, ref p, 1f);
        Assert.Equal(20f, state.Velocity);

        c = Step(follow, ref state, c, ref p, -50f);
        Assert.True(state.Tracking);
        Assert.Equal(-1, state.Side);
        Assert.True(state.Carry);
        Assert.Equal(19f, state.Velocity, 3);
    }

    [Fact]
    public void CAM21_ForwardReachesThePlayerThenLeads()
    {
        var forward = NewForward(20f, 10f, 100f, 30f);
        var state = new CameraAxisState();
        var p = 19f;
        var c = 0f;

        c = Step(forward, ref state, c, ref p, 1f);
        Assert.Equal(10f, c, 3);

        c = Step(forward, ref state, c, ref p, 1f);
        Assert.Equal(p, c, 3);
        Assert.Equal(1, state.Stage);
        Assert.Equal(100f, state.Velocity);

        for (var i = 0; i < 10; i++)
            c = Step(forward, ref state, c, ref p, 1f);
        Assert.Equal(p + 10f, c, 3);
        Assert.Equal(2, state.Stage);
        Assert.Equal(0f, state.Velocity);
    }
}
