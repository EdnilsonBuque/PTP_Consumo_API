using System.Text.Json;
using System.Text.Json.Serialization;

const string weatherEndpoint =
    "https://api.open-meteo.com/v1/forecast?latitude=52.52&longitude=13.41&current=temperature_2m,wind_speed_10m";
const string bibleEndpoint = "https://bible-api.com/john%203:16";
const string jokeEndpoint = "https://deviens-marrant.fr/api/widget/v1/vanne-du-jour";

using var httpClient = new HttpClient();

while (true)
{
    Console.WriteLine("=== Consumo de Web APIs ===");
    Console.WriteLine("1 - Meteorologia (Open-Meteo)");
    Console.WriteLine("2 - Bíblia (João 3:16)");
    Console.WriteLine("3 - Piada do dia (API francesa)");
    Console.WriteLine("0 - Sair");
    Console.Write("Escolha uma opção: ");

    var choice = Console.ReadLine();
    if (choice == "0")
    {
        break;
    }

    try
    {
        switch (choice)
        {
            case "1":
                await ShowWeatherAsync();
                break;
            case "2":
                await ShowBibleVerseAsync();
                break;
            case "3":
                await ShowJokeAsync();
                break;
            default:
                Console.WriteLine("Opção inválida. Escolha 1, 2, 3 ou 0.");
                break;
        }
    }
    catch (HttpRequestException exception)
    {
        Console.Error.WriteLine($"Falha na comunicação com a API: {exception.Message}");
    }
    catch (TaskCanceledException exception)
    {
        Console.Error.WriteLine($"A requisição foi cancelada ou excedeu o tempo limite: {exception.Message}");
    }
    catch (JsonException exception)
    {
        Console.Error.WriteLine($"Não foi possível processar a resposta JSON: {exception.Message}");
    }

    Console.WriteLine();
}

async Task ShowWeatherAsync()
{
    Console.WriteLine("\nMeteorologia atual em Berlim:");

    var weather = await GetJsonAsync<WeatherResponse>(weatherEndpoint);
    if (weather.Current is null ||
        weather.Current.Temperature is null ||
        weather.Current.WindSpeed is null ||
        weather.CurrentUnits?.Temperature is null ||
        weather.CurrentUnits.WindSpeed is null)
    {
        throw new JsonException("A resposta da API não contém os dados meteorológicos esperados.");
    }

    Console.WriteLine($"Hora: {weather.Current.Time}");
    Console.WriteLine($"Temperatura: {weather.Current.Temperature:F1} {weather.CurrentUnits.Temperature}");
    Console.WriteLine($"Velocidade do vento: {weather.Current.WindSpeed:F1} {weather.CurrentUnits.WindSpeed}");
}

async Task ShowBibleVerseAsync()
{
    Console.WriteLine("\nBíblia - João 3:16:");

    var verse = await GetJsonAsync<BibleResponse>(bibleEndpoint);
    if (string.IsNullOrWhiteSpace(verse.Text))
    {
        throw new JsonException("A resposta da API não contém o texto do versículo.");
    }

    Console.WriteLine(verse.Text.Trim());
    Console.WriteLine($"Tradução: {verse.TranslationName}");
}

async Task ShowJokeAsync()
{
    var joke = await GetJsonAsync<JokeResponse>(jokeEndpoint);
    if (string.IsNullOrWhiteSpace(joke.Setup) ||
        string.IsNullOrWhiteSpace(joke.Punchline) ||
        joke.Attribution is null ||
        string.IsNullOrWhiteSpace(joke.Attribution.Name) ||
        string.IsNullOrWhiteSpace(joke.Attribution.Url))
    {
        throw new JsonException("A resposta da API não contém a piada ou a atribuição esperada.");
    }

    Console.WriteLine($"\nPiada do dia ({joke.Date}):");
    Console.WriteLine(joke.Setup);
    Console.WriteLine(joke.Punchline);
    Console.WriteLine($"Fonte: {joke.Attribution.Name} - {joke.Attribution.Url}");
}

async Task<T> GetJsonAsync<T>(string endpoint) where T : class
{
    using var response = await httpClient.GetAsync(endpoint);
    response.EnsureSuccessStatusCode();

    await using var contentStream = await response.Content.ReadAsStreamAsync();
    return await JsonSerializer.DeserializeAsync<T>(
        contentStream,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new JsonException("A resposta da API está vazia.");
}

internal sealed class WeatherResponse
{
    [JsonPropertyName("current")]
    public CurrentWeather? Current { get; init; }

    [JsonPropertyName("current_units")]
    public WeatherUnits? CurrentUnits { get; init; }
}

internal sealed class CurrentWeather
{
    [JsonPropertyName("time")]
    public string? Time { get; init; }

    [JsonPropertyName("temperature_2m")]
    public double? Temperature { get; init; }

    [JsonPropertyName("wind_speed_10m")]
    public double? WindSpeed { get; init; }
}

internal sealed class WeatherUnits
{
    [JsonPropertyName("temperature_2m")]
    public string? Temperature { get; init; }

    [JsonPropertyName("wind_speed_10m")]
    public string? WindSpeed { get; init; }
}

internal sealed class BibleResponse
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("translation_name")]
    public string? TranslationName { get; init; }
}

internal sealed class JokeResponse
{
    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("amorce")]
    public string? Setup { get; init; }

    [JsonPropertyName("chute")]
    public string? Punchline { get; init; }

    [JsonPropertyName("attribution")]
    public JokeAttribution? Attribution { get; init; }
}

internal sealed class JokeAttribution
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}
