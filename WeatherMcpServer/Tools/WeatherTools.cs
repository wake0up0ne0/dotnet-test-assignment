using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace WeatherMcpServer.Tools;

public class WeatherTools
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherTools> _logger;
    private readonly string? _apiKey;

    public WeatherTools(HttpClient httpClient, ILogger<WeatherTools> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY");
    }

    [McpServerTool]
    [Description("Get current weather conditions for a specific city or location.")]
    public async Task<string> GetCurrentWeather(
        [Description("Name of the city to get current weather for")] string city)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("OpenWeather API key not configured. Set OPENWEATHER_API_KEY environment variable.");
            return "Error: Weather API key not configured. Please set the OPENWEATHER_API_KEY environment variable.";
        }

        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric";
            var response = await _httpClient.GetStringAsync(url);
            var weatherData = JsonSerializer.Deserialize<CurrentWeatherResponse>(response);

            if (weatherData?.Weather != null && weatherData.Weather.Length > 0 && weatherData.Main != null)
            {
                var weather = weatherData.Weather[0];
                var main = weatherData.Main;
                
                return $"Current weather in {weatherData.Name}: {weather.Description} " +
                       $"(Temperature: {main.Temp:F1}°C, Feels like: {main.FeelsLike:F1}°C, " +
                       $"Humidity: {main.Humidity}%, Pressure: {main.Pressure} hPa)";
            }

            return $"Unable to get weather data for {city}";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting weather for {City}", city);
            return $"Error: Unable to fetch weather data for {city}. Please check the city name and try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weather for {City}", city);
            return $"Error: An unexpected error occurred while fetching weather data for {city}.";
        }
    }

    [McpServerTool]
    [Description("Get weather forecast for a specific city (5-day forecast with 3-hour intervals).")]
    public async Task<string> GetWeatherForecast(
        [Description("Name of the city to get weather forecast for")] string city,
        [Description("Number of days to forecast (1-5, default: 3)")] int days = 3)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("OpenWeather API key not configured. Set OPENWEATHER_API_KEY environment variable.");
            return "Error: Weather API key not configured. Please set the OPENWEATHER_API_KEY environment variable.";
        }

        if (days < 1 || days > 5)
        {
            return "Error: Days parameter must be between 1 and 5.";
        }

        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/forecast?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric";
            var response = await _httpClient.GetStringAsync(url);
            var forecastData = JsonSerializer.Deserialize<ForecastResponse>(response);

            if (forecastData?.List != null && forecastData.List.Length > 0)
            {
                var result = $"Weather forecast for {forecastData.City?.Name}:\n\n";
                var currentDate = DateTime.UtcNow.Date;
                var endDate = currentDate.AddDays(days);

                var dailyForecasts = forecastData.List
                    .Where(f => DateTimeExtensions.UnixTimeStampToDateTime(f.Dt).Date >= currentDate && 
                               DateTimeExtensions.UnixTimeStampToDateTime(f.Dt).Date < endDate)
                    .GroupBy(f => DateTimeExtensions.UnixTimeStampToDateTime(f.Dt).Date)
                    .Take(days);

                foreach (var dayGroup in dailyForecasts)
                {
                    var date = dayGroup.Key;
                    var dayForecasts = dayGroup.OrderBy(f => f.Dt).ToList();
                    
                    result += $"{date:dddd, MMMM dd}:\n";
                    
                    foreach (var forecast in dayForecasts.Take(4)) // Show up to 4 forecasts per day
                    {
                        var time = DateTimeExtensions.UnixTimeStampToDateTime(forecast.Dt);
                        var weather = forecast.Weather?[0];
                        var main = forecast.Main;
                        
                        if (weather != null && main != null)
                        {
                            result += $"  {time:HH:mm}: {weather.Description}, {main.Temp:F1}°C (feels like {main.FeelsLike:F1}°C)\n";
                        }
                    }
                    result += "\n";
                }

                return result;
            }

            return $"Unable to get forecast data for {city}";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting forecast for {City}", city);
            return $"Error: Unable to fetch forecast data for {city}. Please check the city name and try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting forecast for {City}", city);
            return $"Error: An unexpected error occurred while fetching forecast data for {city}.";
        }
    }

    [McpServerTool]
    [Description("Get weather alerts and warnings for a specific location (bonus feature).")]
    public async Task<string> GetWeatherAlerts(
        [Description("Name of the city to get weather alerts for")] string city)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("OpenWeather API key not configured. Set OPENWEATHER_API_KEY environment variable.");
            return "Error: Weather API key not configured. Please set the OPENWEATHER_API_KEY environment variable.";
        }

        try
        {
            // First get coordinates for the city
            var geocodeUrl = $"https://api.openweathermap.org/geo/1.0/direct?q={Uri.EscapeDataString(city)}&limit=1&appid={_apiKey}";
            var geocodeResponse = await _httpClient.GetStringAsync(geocodeUrl);
            var locations = JsonSerializer.Deserialize<GeocodeResponse[]>(geocodeResponse);

            if (locations == null || locations.Length == 0)
            {
                return $"Error: Could not find location coordinates for {city}.";
            }

            var location = locations[0];
            
            // Get weather alerts using One Call API
            var alertsUrl = $"https://api.openweathermap.org/data/3.0/onecall?lat={location.Lat}&lon={location.Lon}&appid={_apiKey}&exclude=minutely,hourly,daily";
            var alertsResponse = await _httpClient.GetStringAsync(alertsUrl);
            var alertsData = JsonSerializer.Deserialize<OneCallResponse>(alertsResponse);

            if (alertsData?.Alerts != null && alertsData.Alerts.Length > 0)
            {
                var result = $"Weather alerts for {city}:\n\n";
                
                foreach (var alert in alertsData.Alerts)
                {
                    var startTime = DateTimeExtensions.UnixTimeStampToDateTime(alert.Start);
                    var endTime = DateTimeExtensions.UnixTimeStampToDateTime(alert.End);
                    
                    result += $"🚨 {alert.Event}\n";
                    result += $"   From: {startTime:yyyy-MM-dd HH:mm} UTC\n";
                    result += $"   To: {endTime:yyyy-MM-dd HH:mm} UTC\n";
                    result += $"   Source: {alert.SenderName}\n";
                    result += $"   Description: {alert.Description}\n\n";
                }
                
                return result;
            }
            
            return $"No weather alerts currently active for {city}.";
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("401"))
        {
            _logger.LogError(ex, "Unauthorized access to weather alerts API for {City}", city);
            return $"Error: Weather alerts require a premium API key. Current alerts feature is not available.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting weather alerts for {City}", city);
            return $"Error: Unable to fetch weather alerts for {city}. Please check the city name and try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weather alerts for {City}", city);
            return $"Error: An unexpected error occurred while fetching weather alerts for {city}.";
        }
    }
}

// Data models for OpenWeatherMap API responses
public class CurrentWeatherResponse
{
    public string? Name { get; set; }
    public WeatherInfo[]? Weather { get; set; }
    public MainWeatherData? Main { get; set; }
}

public class ForecastResponse
{
    public ForecastItem[]? List { get; set; }
    public CityInfo? City { get; set; }
}

public class ForecastItem
{
    public long Dt { get; set; }
    public MainWeatherData? Main { get; set; }
    public WeatherInfo[]? Weather { get; set; }
}

public class WeatherInfo
{
    public string? Main { get; set; }
    public string? Description { get; set; }
}

public class MainWeatherData
{
    public double Temp { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public double Pressure { get; set; }
}

public class CityInfo
{
    public string? Name { get; set; }
}

public class GeocodeResponse
{
    public string? Name { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
}

public class OneCallResponse
{
    public WeatherAlert[]? Alerts { get; set; }
}

public class WeatherAlert
{
    public string? SenderName { get; set; }
    public string? Event { get; set; }
    public long Start { get; set; }
    public long End { get; set; }
    public string? Description { get; set; }
}

public static class DateTimeExtensions
{
    public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        return DateTime.UnixEpoch.AddSeconds(unixTimeStamp);
    }
}