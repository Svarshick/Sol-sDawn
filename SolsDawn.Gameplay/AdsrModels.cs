using System;

namespace SolsDawn.Gameplay;

[AdsrModel(AllowedAdsrState.Attack)]
[Default]
public class SimpleAttack : IAdsrModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Acceleration = 10f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Min(targetSpeed, Math.Abs(velocity) + Acceleration * dt);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
[Default]
public class LinearSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Deceleration = 10f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, Math.Abs(velocity) - Deceleration * dt);
    }
}

[AdsrModel(AllowedAdsrState.Sustain)]
[Default]
public class KeepSpeed : IAdsrModel
{
    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Abs(velocity);
    }
}
