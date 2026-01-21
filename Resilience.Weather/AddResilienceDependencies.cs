using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Resilience.WeatherForecast;

public static class Dependencies
{
    public static IServiceCollection AddResilienceDependencies(this IServiceCollection services)
    {
        services.AddResiliencePipeline(
            ResiliencePipelineKey.Database,
            pipeline =>
            {
                //TimeoutRejectedException
                pipeline.AddTimeout(TimeSpan.FromSeconds(10));

                pipeline.AddRetry(
                    new RetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
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

        services.AddResiliencePipeline<ResiliencePipelineKey>(
            ResiliencePipelineKey.ExternalApi,
            pipeline =>
            {
                pipeline.AddTimeout(TimeSpan.FromSeconds(5));
                pipeline.AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 5 });
            }
        );

        return services;
    }
}
