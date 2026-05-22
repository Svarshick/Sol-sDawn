using Stateless;

namespace SolsDawn.Core.Logic;

public static class StatelessExtension
{
    public static StateMachine<TState, TState>.StateConfiguration Permit<TState>(
        this StateMachine<TState, TState>.StateConfiguration configuration, TState destinationState)
    {
        return configuration.Permit(destinationState, destinationState);
    }
}