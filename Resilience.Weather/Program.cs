using Microsoft.Extensions.Http.Resilience;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Resilience.WeatherForecast.Resiliences;

namespace Resilience.WeatherForecast;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSwaggerGen();
        builder.Services.AddScoped<NpgsqlConnection>(sp =>
        {
            return new NpgsqlConnection(
                builder.Configuration.GetValue<string>("PostgreSettings:Connection")
            );
        });
        builder.Services.AddSingleton<IRetryPolicyFactory, RetryPolicyFactory>();
        builder.Services.AddResilienceDependencies();
        builder.Services.AddResiliencePipeline(
            "db-pipeline",
            pipeline =>
            {
                pipeline.AddTimeout(TimeSpan.FromSeconds(8));
                pipeline.AddRetry(
                    new RetryStrategyOptions
                    {
                        MaxRetryAttempts = 20,
                        BackoffType = DelayBackoffType.Exponential,
                    }
                );
                pipeline.AddCircuitBreaker(
                    new CircuitBreakerStrategyOptions
                    {
                        FailureRatio = 0.3,
                        SamplingDuration = TimeSpan.FromSeconds(30),
                        MinimumThroughput = 5,
                        BreakDuration = TimeSpan.FromSeconds(30),
                    }
                );
            }
        );

        builder
            .Services.AddHttpClient(
                "WeatherApi",
                client => client.BaseAddress = new Uri("http://localhost:5237/")
            )
            .AddResilienceHandler(
                "custom",
                pipeline =>
                {
                    pipeline.AddTimeout(TimeSpan.FromSeconds(5));

                    pipeline.AddRetry(
                        new HttpRetryStrategyOptions
                        {
                            MaxRetryAttempts = 20,
                            BackoffType = DelayBackoffType.Exponential,
                            UseJitter = true,
                            Delay = TimeSpan.FromMilliseconds(500),
                        }
                    );

                    pipeline.AddCircuitBreaker(
                        new HttpCircuitBreakerStrategyOptions
                        {
                            FailureRatio = 0.3,
                            SamplingDuration = TimeSpan.FromSeconds(30),
                            MinimumThroughput = 5,
                            BreakDuration = TimeSpan.FromSeconds(30),
                        }
                    );
                }
            );

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        var app = builder.Build();
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
