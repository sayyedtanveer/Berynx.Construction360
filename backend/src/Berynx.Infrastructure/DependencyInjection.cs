using Berynx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Berynx.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:DefaultConnection through environment-based configuration.");
        }

        var migrationsAssembly =
            typeof(BerynxDbContext).Assembly.GetName().Name
            ?? throw new InvalidOperationException(
                "The Infrastructure assembly name could not be resolved.");

        services.AddDbContext<BerynxDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(migrationsAssembly)));
        return services;
    }
}
