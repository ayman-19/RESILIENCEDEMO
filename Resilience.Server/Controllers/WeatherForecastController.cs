using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Resilience.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(
    NpgsqlConnection connection,
    ILogger<WeatherForecastController> logger
) : ControllerBase
{
    static int _counter = 0;
    private static readonly string[] Summaries =
    [
        "Freezing",
        "Bracing",
        "Chilly",
        "Cool",
        "Mild",
        "Warm",
        "Balmy",
        "Hot",
        "Sweltering",
        "Scorching",
    ];

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        if (++_counter == 3)
            throw new Exception("Simulated exception");
        Console.WriteLine(_counter);
        return Enumerable
            .Range(1, 5)
            .Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)],
            })
            .ToArray();
    }

    [HttpGet("copons")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        int pageIndex = 1,
            pageSize = 5;
        string search = "";
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
}
