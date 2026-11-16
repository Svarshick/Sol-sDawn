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

[AdsrModel(AllowedAdsrState.Attack)]
public class SimpleExpectedAttack : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    [Inspect, Range(0f, 200f), Step(1f)]
    public float ExpectedSpeed = 15f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var currentSpeed = Math.Abs(velocity);
        var normalizedTime = dt / ExpectedTime;

        var speed = currentSpeed + ExpectedSpeed * normalizedTime;

        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Min(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Attack)]
public class SquareRootExpectedAttack : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    [Inspect, Range(0f, 200f), Step(1f)]
    public float ExpectedSpeed = 15f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var normalizedSpeed = Math.Abs(velocity) / ExpectedSpeed;
        var normalizedTime = dt / ExpectedTime;

        var speed = MathF.Sqrt(normalizedSpeed * normalizedSpeed + normalizedTime * normalizedTime) * ExpectedSpeed;

        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Min(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Attack)]
public class SquareExpectedAttack : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    [Inspect, Range(0f, 200f), Step(1f)]
    public float ExpectedSpeed = 15f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var normalizedSpeed = Math.Abs(velocity) / ExpectedSpeed;
        var normalizedTime = dt / ExpectedTime;

        var root = MathF.Sqrt(normalizedSpeed) + normalizedTime;
        var speed = root * root * ExpectedSpeed;

        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Min(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class LinearExpectedSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    [Inspect, Range(0f, 200f), Step(1f)]
    public float StartingSpeed = 15f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var normalizedSpeed = Math.Abs(velocity) / StartingSpeed;
        var normalizedTime = dt / ExpectedTime;

        var speed = StartingSpeed * (normalizedSpeed - normalizedTime);

        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class SquareRootExpectedSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    [Inspect, Range(0f, 200f), Step(1f)]
    public float StartingSpeed = 12f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var normalizedSpeed = Math.Abs(velocity) / StartingSpeed;
        var normalizedTime = dt / ExpectedTime;

        var coef = normalizedSpeed * normalizedSpeed - normalizedTime;

        var speed = 0f;
        if (coef >= 0.001f)
            speed = StartingSpeed * MathF.Sqrt(coef);

        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class SquareExpectedSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    [Inspect, Range(0f, 200f), Step(1f)]
    public float StartingSpeed = 12f;

    public float Calculate(float velocity, Sign prevInDir, Sign inDir, float dt, float targetSpeed)
    {
        var normalizedSpeed = Math.Abs(velocity) / StartingSpeed;
        var normalizedTime = dt / ExpectedTime;

        var root = MathF.Sqrt(normalizedSpeed) - normalizedTime;
        var speed = StartingSpeed * root * root;

        var dir = inDir != 0 ? (int)inDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, speed);
    }
}
