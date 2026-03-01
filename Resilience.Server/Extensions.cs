namespace Resilience.Server;

public static class Extensions
{
    public static IServiceScope CreateScope(this IServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
}
