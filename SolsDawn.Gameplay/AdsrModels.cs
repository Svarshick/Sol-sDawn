namespace SolsDawn.Gameplay;

[AdsrModel(AllowedAdsrState.Attack)]
[Default]
public class SimpleAttack : IAdsrModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Acceleration = 10f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed) =>
        Min(toSpeed, speed + Acceleration * dt);
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
[Default]
public class LinearSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 200f), Step(1f)]
    public float Deceleration = 10f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed) =>
        Max(toSpeed, speed - Deceleration * dt);
}

[AdsrModel(AllowedAdsrState.Attack)]
public class SimpleExpectedAttack : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed)
    {
        var range = toSpeed - fromSpeed;
        if (range <= 0f || ExpectedTime <= 0f)
            return toSpeed;

        return Min(toSpeed, speed + range * dt / ExpectedTime);
    }
}

[AdsrModel(AllowedAdsrState.Attack)]
public class SquareRootExpectedAttack : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 0.2f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed)
    {
        var range = toSpeed - fromSpeed;
        if (range <= 0f || ExpectedTime <= 0f)
            return toSpeed;

        var normalizedSpeed = Max(0f, speed - fromSpeed) / range;
        var normalizedTime = dt / ExpectedTime;
        var next = Sqrt(normalizedSpeed * normalizedSpeed + normalizedTime);

        return Min(toSpeed, fromSpeed + next * range);
    }
}

[AdsrModel(AllowedAdsrState.Attack)]
public class SquareExpectedAttack : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 0.3f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed)
    {
        var range = toSpeed - fromSpeed;
        if (range <= 0f || ExpectedTime <= 0f)
            return toSpeed;

        var normalizedSpeed = Max(0f, speed - fromSpeed) / range;
        var root = Sqrt(normalizedSpeed) + dt / ExpectedTime;

        return Min(toSpeed, fromSpeed + root * root * range);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class LinearExpectedSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 1f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed)
    {
        var range = Max(fromSpeed, speed) - toSpeed;
        if (range <= 0f || ExpectedTime <= 0f)
            return toSpeed;

        return Max(toSpeed, speed - range * dt / ExpectedTime);
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class SquareRootExpectedSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 0.3f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed)
    {
        var range = Max(fromSpeed, speed) - toSpeed;
        if (range <= 0f || ExpectedTime <= 0f)
            return toSpeed;

        var normalizedSpeed = (speed - toSpeed) / range;
        var coef = normalizedSpeed * normalizedSpeed - dt / ExpectedTime;
        if (coef < 0.001f)
            return toSpeed;

        return Max(toSpeed, toSpeed + range * Sqrt(coef));
    }
}

[AdsrModel(AllowedAdsrState.Decay | AllowedAdsrState.Release)]
public class SquareExpectedSlowdown : IAdsrModel
{
    [Inspect, Range(0f, 3f), Step(0.05f)]
    public float ExpectedTime = 0.1f;

    public float Calculate(float dt, float speed, float fromSpeed, float toSpeed)
    {
        var range = Max(fromSpeed, speed) - toSpeed;
        if (range <= 0f || ExpectedTime <= 0f)
            return toSpeed;

        var normalizedSpeed = (speed - toSpeed) / range;
        var root = Sqrt(normalizedSpeed) - dt / ExpectedTime;
        var res = root * root * range;
        if (res <= 0.1f)
            return toSpeed;

        return Max(toSpeed, toSpeed + res);
    }
}
