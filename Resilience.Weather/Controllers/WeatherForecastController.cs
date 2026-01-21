using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Polly;
using Resilience.WeatherForecast.Resiliences;

namespace Resilience.WeatherForecast.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController(
        IHttpClientFactory factory,
        NpgsqlConnection connection,
        IRetryPolicyFactory retryPolicyFactory,
        ILogger<WeatherForecastController> logger
    ) : ControllerBase
    {
        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IActionResult> Get()
        {
            var client = factory.CreateClient("WeatherApi");
            var result = await client.GetAsync("WeatherForecast");

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();
            return Ok(content);
        }

        [HttpGet(Name = "GetWeatherForecast2")]
        public async Task<IActionResult> Get3()
        {
            var client = factory.CreateClient("WeatherApi");
            var result = await client.GetAsync("WeatherForecast/copons");

            result.EnsureSuccessStatusCode();

            var content = await result.Content.ReadAsStringAsync();
            return Ok(content);
        }

        [HttpGet("copons/{pageIndex}/{pageSize}/{search}")]
        public async Task<IActionResult> Get(
            int pageIndex,
            int pageSize,
            string search,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                const string sql = """
                    SELECT 
                           ProductName,
                           Description,
                           Amount
                    FROM Coupon
                    WHERE (@Search IS NULL OR ProductName ILIKE '%' || @Search || '%')
                    ORDER BY ProductName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;
                    """;

                logger.LogInformation(
                    "Fetching coupons | Search={Search}, PageIndex={PageIndex}, PageSize={PageSize}",
                    string.IsNullOrWhiteSpace(search) ? "NULL" : search,
                    pageIndex,
                    pageSize
                );

                var result = await connection.QueryAsync(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            Search = string.IsNullOrWhiteSpace(search) ? null : search,
                            Offset = (pageIndex - 1) * pageSize,
                            PageSize = pageSize,
                        },
                        cancellationToken: cancellationToken
                    )
                );

                //logger.LogInformation(
                //	"Fetching coupons | Search={Search}, PageIndex={PageIndex}, PageSize={PageSize}",
                //	string.IsNullOrWhiteSpace(search) ? "NULL" : search,
                //	pageIndex,
                //	pageSize
                //);

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error occurred while fetching coupons | Search={Search}, PageIndex={PageIndex}, PageSize={PageSize}",
                    string.IsNullOrWhiteSpace(search) ? "NULL" : search,
                    pageIndex,
                    pageSize
                );
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("copons2/{pageIndex}/{pageSize}/{search}")]
        public async Task<IActionResult> Get2(
            int pageIndex,
            int pageSize,
            string search,
            CancellationToken cancellationToken = default
        )
        {
            var retryPolicy = Policy
                .Handle<NpgsqlException>()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(500 * attempt));

            var result1 = await retryPolicy.ExecuteAsync(async () =>
            {
                const string sql = """
                SELECT 
                       ProductName,
                       Description,
                       Amount
                FROM Coupon
                WHERE (@Search IS NULL OR ProductName ILIKE '%' || @Search || '%')
                ORDER BY ProductName
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;
                """;

                logger.LogInformation(
                    "Fetching coupons | Search={Search}, PageIndex={PageIndex}, PageSize={PageSize}",
                    string.IsNullOrWhiteSpace(search) ? "NULL" : search,
                    pageIndex,
                    pageSize
                );

                var result = await connection.QueryAsync(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            Search = string.IsNullOrWhiteSpace(search) ? null : search,
                            Offset = (pageIndex - 1) * pageSize,
                            PageSize = pageSize,
                        },
                        cancellationToken: cancellationToken
                    )
                );

                //logger.LogInformation(
                //	"Fetching coupons | Search={Search}, PageIndex={PageIndex}, PageSize={PageSize}",
                //	string.IsNullOrWhiteSpace(search) ? "NULL" : search,
                //	pageIndex,
                //	pageSize
                //);

                return result;
            });
            return Ok(result1);
        }

        [HttpGet("copons3/{pageIndex}/{pageSize}/{search}")]
        public async Task<IEnumerable<dynamic>> GetCouponsAsync(
            int pageIndex,
            int pageSize,
            string? search,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var retryPolicy = retryPolicyFactory.CreateRetryPolicy<NpgsqlException>();

                return await retryPolicy.ExecuteAsync(async () =>
                {
                    const string sql = """
                    SELECT 
                           ProductName,
                           Description,
                           Amount
                    FROM Coupon
                    WHERE (@Search IS NULL OR ProductName ILIKE '%' || @Search || '%')
                    ORDER BY ProductName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY;
                    """;

                    logger.LogInformation(
                        "Fetching coupons | Search={Search}, PageIndex={PageIndex}, PageSize={PageSize}",
                        string.IsNullOrWhiteSpace(search) ? "NULL" : search,
                        pageIndex,
                        pageSize
                    );

                    var result = await connection.QueryAsync(
                        new CommandDefinition(
                            sql,
                            new
                            {
                                Search = string.IsNullOrWhiteSpace(search) ? null : search,
                                Offset = (pageIndex - 1) * pageSize,
                                PageSize = pageSize,
                            },
                            cancellationToken: cancellationToken
                        )
                    );

                    logger.LogInformation(
                        "Successfully fetched {Count} coupons",
                        result.AsList().Count
                    );

                    return result;
                });
            }
            catch
            {
                return Enumerable.Empty<dynamic>();
            }
        }
    }
}
