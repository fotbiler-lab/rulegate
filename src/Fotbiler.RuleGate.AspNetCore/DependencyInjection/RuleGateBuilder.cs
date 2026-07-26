using Microsoft.Extensions.DependencyInjection;

namespace Fotbiler.RuleGate.AspNetCore.DependencyInjection;

public sealed class RuleGateBuilder
{
    internal RuleGateBuilder(
        IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Services = services;
    }

    public IServiceCollection Services { get; }
}
