using System;

namespace SolsDawn.Gameplay;

[AdsrModel(AllowedAdsrState.Attack)]
[Default]
public class SimpleAttack : IAdsrModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Acceleration = 10f;

    public float Calculate(float dt, float startVelocity, float velocity, float targetSpeed, Sign prevInputDir, Sign inputDir)
    {
        var dir = inputDir != 0 ? (int)inputDir : Math.Sign(velocity);
        return dir * Math.Min(targetSpeed, Math.Abs(velocity) + Acceleration * dt);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
[Default]
public class LinearSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Deceleration = 10f;

    public float Calculate(float dt, float startVelocity, float velocity, float targetSpeed, Sign prevInputDir, Sign inputDir)
    {
        var dir = inputDir != 0 ? (int)inputDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, Math.Abs(velocity) - Deceleration * dt);
    }
}

[AdsrModel(AllowedAdsrState.Sustain)]
[Default]
public class KeepSpeed : IAdsrModel
{
    public float Calculate(float dt, float startVelocity, float velocity, float targetSpeed, Sign prevInputDir, Sign inputDir)
    {
        var dir = inputDir != 0 ? (int)inputDir : Math.Sign(velocity);
        return dir * Math.Abs(velocity);
    }
}

[AdsrModel(AllowedAdsrState.Attack)]
public class SimpleExpectedAttack : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    public float Calculate(float dt, float startVelocity, float velocity, float targetSpeed, Sign prevInputDir, Sign inputDir)
    {
        var currentSpeed = Math.Abs(velocity);
        var normalizedTime = dt / ExpectedTime;

        var speed = currentSpeed + targetSpeed * normalizedTime;

        var dir = inputDir != 0 ? (int)inputDir : Math.Sign(velocity);
        return dir * Math.Min(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Attack)]
public class SquareRootExpectedAttack : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    public float Calculate(float dt, float startVelocity, float velocity, float targetSpeed, Sign prevInputDir, Sign inputDir)
    {
        var normalizedSpeed = Math.Abs(velocity) / targetSpeed;
        var normalizedTime = dt / ExpectedTime;

        var speed = MathF.Sqrt(normalizedSpeed * normalizedSpeed + normalizedTime * normalizedTime) * targetSpeed;

        var dir = inputDir != 0 ? (int)inputDir : Math.Sign(velocity);
        return dir * Math.Min(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Attack)]
public class SquareExpectedAttack : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    public float Calculate(float dt, float startVelocity, float velocity, float targetSpeed, Sign prevInputDir, Sign inputDir)
    {
        var normalizedSpeed = Math.Abs(velocity) / targetSpeed;
        var normalizedTime = dt / ExpectedTime;

        var root = MathF.Sqrt(normalizedSpeed) + normalizedTime;
        var speed = root * root * targetSpeed;

        var dir = inputDir != 0 ? (int)inputDir : Math.Sign(velocity);
        return dir * Math.Min(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class LinearExpectedSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    public float Calculate(float dt, float startVelocity, float velocity, float targetSpeed, Sign prevInputDir, Sign inputDir)
    {
        var startSpeed = Math.Abs(startVelocity);
        if (startSpeed == 0f)
            return 0f;

        var normalizedSpeed = Math.Abs(velocity) / startSpeed;
        var normalizedTime = dt / ExpectedTime;

        var speed = startSpeed * (normalizedSpeed - normalizedTime);

        var dir = inputDir != 0 ? (int)inputDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class SquareRootExpectedSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    public float Calculate(float dt, float startVelocity, float velocity, float targetSpeed, Sign prevInputDir, Sign inputDir)
    {
        var startSpeed = Math.Abs(startVelocity);
        if (startSpeed == 0f)
            return 0f;

        var normalizedSpeed = Math.Abs(velocity) / startSpeed;
        var normalizedTime = dt / ExpectedTime;

        var coef = normalizedSpeed * normalizedSpeed - normalizedTime;

        var speed = 0f;
        if (coef >= 0.001f)
            speed = startSpeed * MathF.Sqrt(coef);

        var dir = inputDir != 0 ? (int)inputDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, speed);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class SquareExpectedSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    public float Calculate(float dt, float startVelocity, float velocity, float targetSpeed, Sign prevInputDir, Sign inputDir)
    {
        var startSpeed = Math.Abs(startVelocity);
        if (startSpeed == 0f)
            return 0f;

        var normalizedSpeed = Math.Abs(velocity) / startSpeed;
        var normalizedTime = dt / ExpectedTime;

        var root = MathF.Sqrt(normalizedSpeed) - normalizedTime;
        var speed = startSpeed * root * root;

        var dir = inputDir != 0 ? (int)inputDir : Math.Sign(velocity);
        return dir * Math.Max(targetSpeed, speed);
    }
}
