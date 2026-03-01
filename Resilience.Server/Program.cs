using Npgsql;

namespace Resilience.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddScoped<NpgsqlConnection>(sp =>
        {
            return new NpgsqlConnection(
                builder.Configuration.GetValue<string>("PostgreSettings:Connection")
            );
        });
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        var app = builder.Build();

        using var scope = app.Services.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        connection.Open();

        connection.Close();
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
