using Mapster;

namespace _______
{
    public class WeatherForecastMapping : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config
                .NewConfig<WeatherForecast, WeatherForecastDto>()
                .Map(dest => dest.TemperatureF, src => 32 + (int)(src.TemperatureC / 0.5556));
        }
    }
}
