using System;

namespace SolsDawn.Core.Logic.Configs;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class UnitsAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class EulerAttribute : Attribute
{
}