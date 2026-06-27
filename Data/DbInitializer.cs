namespace ascent_app.Data;

/// <summary>
/// Backward-compatible entry point. New startup code should call <see cref="Seeders.DatabaseSeeder"/>.
/// </summary>
public static class DbInitializer
{
    public static Task SeedAsync(IServiceProvider services) =>
        Seeders.DatabaseSeeder.SeedAsync(services);
}
