using Polly;

namespace Resilience.WeatherForecast.Resiliences;

public interface IRetryPolicyFactory
{
    IAsyncPolicy CreateRetryPolicy<TException>(int maxRetries = 3)
        where TException : Exception;
    ResiliencePipeline Get(ResiliencePipelineKey key);
}
