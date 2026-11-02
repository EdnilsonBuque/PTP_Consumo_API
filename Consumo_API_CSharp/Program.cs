using System.Text.Json;
using System.Text.Json.Serialization;

const string bibleBaseUrl = "https://bible-api.com/";
const string jokeApiBaseUrl = "https://v2.jokeapi.dev/joke/Any?lang=pt";
const string jokeApiAttribution = "https://v2.jokeapi.dev/";
const string bibleTranslation = "João Ferreira de Almeida";
var newTestamentBookIds = new HashSet<string>(
    [
        "MAT", "MRK", "LUK", "JHN", "ACT", "ROM", "1CO", "2CO", "GAL",
        "EPH", "PHP", "COL", "1TH", "2TH", "1TI", "2TI", "TIT", "PHM",
        "HEB", "JAS", "1PE", "2PE", "1JN", "2JN", "3JN", "JUD", "REV"
    ],
    StringComparer.OrdinalIgnoreCase);

using var httpClient = new HttpClient();
var favorites = new List<SavedItem>();

while (true)
{
    ClearScreen();
    WriteHeader("BÍBLIA & HUMOR", "Leitura e piadas em português");
    WriteMenuOption("1", "Bíblia", "Livros, capítulos e versículos");
    WriteMenuOption("2", "Piadas", "Temas, modo seguro e sorteio");
    WriteMenuOption("3", "Favoritos", $"Guardados nesta sessão: {favorites.Count}");
    WriteMenuOption("0", "Sair");

    var choice = ReadChoice("A sua opção: ", 3);
    if (choice is null or 0)
    {
        break;
    }

    try
    {
        switch (choice)
        {
            case 1:
                await ChooseBibleFeatureAsync();
                break;
            case 2:
                await ChooseJokeFeatureAsync();
                break;
            case 3:
                ShowFavorites();
                break;
        }
    }
    catch (HttpRequestException exception)
    {
        WriteError($"Falha na comunicação com a API: {exception.Message}");
    }
    catch (TaskCanceledException exception)
    {
        WriteError($"A requisição foi cancelada ou excedeu o tempo limite: {exception.Message}");
    }
    catch (JsonException exception)
    {
        WriteError($"Não foi possível processar a resposta JSON: {exception.Message}");
    }

    PauseForMenu();
    if (Console.ReadLine() is null)
    {
        break;
    }
}

async Task ChooseBibleFeatureAsync()
{
    ClearScreen();
    WriteHeader("BÍBLIA · JOÃO FERREIRA DE ALMEIDA", "Escolha uma opção");
    WriteMenuOption("1", "Versículo aleatório", "Uma passagem diferente da Bíblia");
    WriteMenuOption("2", "João 3:16", "Versículo conhecido");
    WriteMenuOption("3", "Salmo 23 completo", "Leia o capítulo inteiro");
    WriteMenuOption("4", "Pesquisar referência", "Ex.: João 3:16 ou Provérbios 3:5");
    WriteMenuOption("5", "Novo Testamento", "Escolha livro, capítulo e versículo");
    WriteMenuOption("0", "Voltar");

    var choice = ReadChoice("A sua opção: ", 5);
    if (choice is null or 0)
    {
        return;
    }

    switch (choice)
    {
        case 1:
        {
            var result = await GetJsonAsync<RandomVerseResponse>(
                $"{bibleBaseUrl}data/almeida/random");
            if (result.RandomVerse is null || string.IsNullOrWhiteSpace(result.RandomVerse.Text))
            {
                throw new JsonException("A resposta não contém o versículo aleatório esperado.");
            }

            var title = $"{result.RandomVerse.Book} {result.RandomVerse.Chapter}:{result.RandomVerse.Verse}";
            ShowBibleText(title, result.RandomVerse.Text);
            OfferToSave(title, result.RandomVerse.Text, bibleTranslation);
            break;
        }
        case 2:
        {
            var result = await GetBiblePassageAsync("João 3:16");
            ShowBibleText(result.Reference ?? "João 3:16", result.Text!);
            OfferToSave(result.Reference ?? "João 3:16", result.Text!, bibleTranslation);
            break;
        }
        case 3:
        {
            var result = await GetJsonAsync<BibleChapterResponse>(
                $"{bibleBaseUrl}data/almeida/PSA/23");
            if (result.Verses is not { Count: > 0 })
            {
                throw new JsonException("A resposta não contém os versículos do Salmo 23.");
            }

            await ShowBibleVersesAsync("Salmos", 23, result.Verses);
            break;
        }
        case 4:
        {
            Console.Write("\nEscreva a referência bíblica: ");
            var reference = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(reference))
            {
                WriteError("A referência não pode estar vazia.");
                return;
            }

            var result = await GetBiblePassageAsync(reference);
            ShowBibleText(result.Reference ?? reference, result.Text!);
            OfferToSave(result.Reference ?? reference, result.Text!, bibleTranslation);
            break;
        }
        case 5:
            await BrowseNewTestamentAsync();
            break;
    }
}

async Task BrowseNewTestamentAsync()
{
    var index = await GetJsonAsync<BibleIndexResponse>(
        $"{bibleBaseUrl}data/almeida");
    var books = index.Books?
        .Where(book => newTestamentBookIds.Contains(book.Id))
        .ToList();

    if (books is not { Count: > 0 })
    {
        throw new JsonException("A resposta não contém os livros do Novo Testamento.");
    }

    var bookIndex = SelectFromPagedList(
        "NOVO TESTAMENTO",
        "Selecione o livro",
        books,
        book => book.Name);
    if (bookIndex is null)
    {
        return;
    }

    var selectedBook = books[bookIndex.Value];
    var bookData = await GetJsonAsync<BibleBookResponse>(
        $"{bibleBaseUrl}data/almeida/{selectedBook.Id}");
    if (bookData.Chapters is not { Count: > 0 } chapters)
    {
        throw new JsonException($"Não foi possível obter os capítulos de {selectedBook.Name}.");
    }

    var chapterIndex = SelectFromPagedList(
        selectedBook.Name.ToUpperInvariant(),
        $"Selecione o capítulo · {chapters.Count} capítulo(s)",
        chapters,
        chapter => $"Capítulo {chapter.Chapter}");
    if (chapterIndex is null)
    {
        return;
    }

    var selectedChapter = chapters[chapterIndex.Value].Chapter;
    var chapterData = await GetJsonAsync<BibleChapterResponse>(
        $"{bibleBaseUrl}data/almeida/{selectedBook.Id}/{selectedChapter}");
    if (chapterData.Verses is not { Count: > 0 } verses)
    {
        throw new JsonException(
            $"Não foi possível obter os versículos de {selectedBook.Name} {selectedChapter}.");
    }

    await ShowBibleVersesAsync(selectedBook.Name, selectedChapter, verses);
}

async Task ShowBibleVersesAsync(string bookName, int chapterNumber, List<BibleVerse> verses)
{
    const int pageSize = 12;
    var pageCount = (int)Math.Ceiling(verses.Count / (double)pageSize);
    var page = 0;

    while (true)
    {
        ClearScreen();
        WriteHeader(
            $"{bookName.ToUpperInvariant()} {chapterNumber}",
            $"{bibleTranslation} · Página {page + 1} de {pageCount}");

        var pageVerses = verses.Skip(page * pageSize).Take(pageSize).ToList();
        foreach (var verse in pageVerses)
        {
            WriteNumberedVerse(verse.Verse, verse.Text);
        }

        Console.WriteLine();
        if (page > 0)
        {
            WriteMenuOption("P", "Página anterior");
        }

        if (page + 1 < pageCount)
        {
            WriteMenuOption("N", "Página seguinte");
        }

        WriteMenuOption("0", "Voltar");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Versículo para guardar ou ação: ");
        Console.ResetColor();

        var input = Console.ReadLine()?.Trim();
        if (input is null or "0")
        {
            return;
        }

        if (string.Equals(input, "N", StringComparison.OrdinalIgnoreCase) && page + 1 < pageCount)
        {
            page++;
            continue;
        }

        if (string.Equals(input, "P", StringComparison.OrdinalIgnoreCase) && page > 0)
        {
            page--;
            continue;
        }

        if (int.TryParse(input, out var verseNumber) &&
            pageVerses.FirstOrDefault(verse => verse.Verse == verseNumber) is { } selectedVerse)
        {
            var reference = $"{bookName} {chapterNumber}:{selectedVerse.Verse}";
            OfferToSave(reference, selectedVerse.Text.Trim(), bibleTranslation);
            return;
        }

        WriteError("Escolha o número de um versículo desta página, N, P ou 0.");
        await Task.Delay(900);
    }
}

int? SelectFromPagedList<T>(
    string title,
    string subtitle,
    IReadOnlyList<T> items,
    Func<T, string> getLabel)
{
    const int pageSize = 9;
    var pageCount = (int)Math.Ceiling(items.Count / (double)pageSize);
    var page = 0;

    while (true)
    {
        ClearScreen();
        WriteHeader(title, $"{subtitle} · Página {page + 1} de {pageCount}");

        var startIndex = page * pageSize;
        var itemCount = Math.Min(pageSize, items.Count - startIndex);
        for (var index = 0; index < itemCount; index++)
        {
            WriteMenuOption((index + 1).ToString(), getLabel(items[startIndex + index]));
        }

        if (page > 0)
        {
            WriteMenuOption("P", "Página anterior");
        }

        if (page + 1 < pageCount)
        {
            WriteMenuOption("N", "Página seguinte");
        }

        WriteMenuOption("0", "Voltar");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Seleção (número, N, P ou 0): ");
        Console.ResetColor();

        var input = Console.ReadLine()?.Trim();
        if (input is null or "0")
        {
            return null;
        }

        if (string.Equals(input, "N", StringComparison.OrdinalIgnoreCase) && page + 1 < pageCount)
        {
            page++;
            continue;
        }

        if (string.Equals(input, "P", StringComparison.OrdinalIgnoreCase) && page > 0)
        {
            page--;
            continue;
        }

        if (int.TryParse(input, out var choice) && choice > 0 && choice <= itemCount)
        {
            return startIndex + choice - 1;
        }

        WriteError($"Escolha um número entre 1 e {itemCount}, N, P ou 0.");
        Thread.Sleep(900);
    }
}

async Task<BibleResponse> GetBiblePassageAsync(string reference)
{
    var encodedReference = Uri.EscapeDataString(reference);
    var result = await GetJsonAsync<BibleResponse>(
        $"{bibleBaseUrl}{encodedReference}?translation=almeida");

    if (string.IsNullOrWhiteSpace(result.Text))
    {
        throw new JsonException("A resposta não contém o texto bíblico esperado.");
    }

    return result;
}

async Task ChooseJokeFeatureAsync()
{
    ClearScreen();
    WriteHeader("PIADAS EM PORTUGUÊS", "Escolha um tema ou deixe a API sortear");
    WriteMenuOption("1", "Piada aleatória", "A API escolhe entre as piadas em português");
    WriteMenuOption("2", "Alimentação e natureza", "Tema do cheiro verde");
    WriteMenuOption("3", "Música", "Tema do pagode");
    WriteMenuOption("4", "Modo seguro", "Evita temas religiosos e conteúdo explícito");
    WriteMenuOption("0", "Voltar");

    var choice = ReadChoice("A sua opção: ", 4);
    if (choice is null or 0)
    {
        return;
    }

    var endpoint = choice switch
    {
        1 => jokeApiBaseUrl,
        2 => $"{jokeApiBaseUrl}&idRange=0-0",
        3 => $"{jokeApiBaseUrl}&idRange=1-1",
        4 => $"{jokeApiBaseUrl}&safe-mode",
        _ => throw new InvalidOperationException("Opção de piada inválida.")
    };

    var joke = await GetJsonAsync<JokeResponse>(endpoint);
    if (joke.Error ||
        joke.Language != "pt" ||
        string.IsNullOrWhiteSpace(joke.Setup) ||
        string.IsNullOrWhiteSpace(joke.Delivery))
    {
        throw new JsonException("A API não devolveu uma piada válida em português.");
    }

    var theme = choice switch
    {
        2 => "Alimentação e natureza",
        3 => "Música",
        4 => "Modo seguro",
        _ => "Sorteio"
    };

    ClearScreen();
    WriteHeader($"PIADA · {theme.ToUpperInvariant()}", $"Português · JokeAPI · #{joke.Id}");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(WrapText(joke.Setup, 68));
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(WrapText(joke.Delivery, 68));
    Console.ResetColor();
    WriteLabel("Fonte", jokeApiAttribution);

    OfferToSave(
        $"Piada · {theme}",
        $"{joke.Setup}{Environment.NewLine}{joke.Delivery}",
        jokeApiAttribution);
}

void ShowBibleText(string title, string text)
{
    ClearScreen();
    WriteHeader(title.ToUpperInvariant(), bibleTranslation);
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(WrapText(text.Trim(), 68));
    Console.ResetColor();
}

void WriteNumberedVerse(int number, string text)
{
    var lines = WrapText(text.Trim(), 64)
        .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"{number,3}. ");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(lines[0]);

    foreach (var line in lines.Skip(1))
    {
        Console.WriteLine($"     {line}");
    }

    Console.WriteLine();
    Console.ResetColor();
}

void OfferToSave(string title, string content, string source)
{
    Console.Write("\nGuardar nos favoritos? (S/N): ");
    var answer = Console.ReadLine()?.Trim();
    if (!string.Equals(answer, "s", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    favorites.Add(new SavedItem(title, content, source));
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Adicionado aos favoritos desta sessão.");
    Console.ResetColor();
}

void ShowFavorites()
{
    ClearScreen();
    WriteHeader("OS MEUS FAVORITOS", "Guardados apenas durante esta execução");
    if (favorites.Count == 0)
    {
        Console.WriteLine("Ainda não guardou nenhum versículo ou piada.");
        return;
    }

    for (var index = 0; index < favorites.Count; index++)
    {
        var favorite = favorites[index];
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n{index + 1}. {favorite.Title}");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(WrapText(favorite.Content, 68));
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"   Fonte: {favorite.Source}");
        Console.ResetColor();
    }

    Console.WriteLine();
    WriteMenuOption("0", "Voltar");
    var choice = ReadChoice("Número do favorito a remover (0 para voltar): ", favorites.Count);
    if (choice is > 0)
    {
        favorites.RemoveAt(choice.Value - 1);
        Console.WriteLine("Favorito removido.");
    }
}

async Task<T> GetJsonAsync<T>(string endpoint) where T : class
{
    const int maxAttempts = 3;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        using var response = await httpClient.GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(
                contentStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("A resposta da API está vazia.");
        }

        var isTemporaryFailure =
            response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
            response.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
            (int)response.StatusCode >= 500;

        if (isTemporaryFailure && attempt < maxAttempts)
        {
            var retryDelay = GetRetryDelay(response, attempt);
            if (retryDelay <= TimeSpan.FromSeconds(30))
            {
                Console.WriteLine(
                    $"\nA API está temporariamente indisponível. Nova tentativa em {retryDelay.TotalSeconds:0} segundo(s) ({attempt}/{maxAttempts - 1})...");
                await Task.Delay(retryDelay);
                continue;
            }
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        var details = string.IsNullOrWhiteSpace(errorContent)
            ? string.Empty
            : $" Resposta: {errorContent}";
        var retryAfter = response.Headers.RetryAfter?.ToString();
        if (!string.IsNullOrWhiteSpace(retryAfter))
        {
            details += $" A API pede para tentar novamente após: {retryAfter}.";
        }

        var errorMessage = isTemporaryFailure
            ? $"A API continua temporariamente indisponível após {attempt} tentativa(s)."
            : "A API devolveu um erro.";
        throw new HttpRequestException(
            $"{errorMessage} HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).{details}");
    }

    throw new HttpRequestException("A API não respondeu após várias tentativas.");
}

TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
{
    var retryAfter = response.Headers.RetryAfter;
    if (retryAfter?.Delta is { } retryAfterDelay)
    {
        return retryAfterDelay > TimeSpan.Zero ? retryAfterDelay : TimeSpan.Zero;
    }

    if (retryAfter?.Date is { } retryAfterDate)
    {
        var retryAfterDateDelay = retryAfterDate - DateTimeOffset.UtcNow;
        return retryAfterDateDelay > TimeSpan.Zero ? retryAfterDateDelay : TimeSpan.Zero;
    }

    return TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
}

int? ReadChoice(string prompt, int maximum)
{
    while (true)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(prompt);
        Console.ResetColor();

        var input = Console.ReadLine();
        if (input is null)
        {
            return null;
        }

        if (int.TryParse(input, out var choice) && choice >= 0 && choice <= maximum)
        {
            return choice;
        }

        WriteError($"Escolha um número entre 0 e {maximum}.");
    }
}

void WriteHeader(string title, string subtitle)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╭" + new string('─', 70) + "╮");
    Console.WriteLine($"│  {title,-68}│");
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine($"│  {subtitle,-68}│");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╰" + new string('─', 70) + "╯");
    Console.ResetColor();
    Console.WriteLine();
}

void ClearScreen()
{
    if (!Console.IsOutputRedirected)
    {
        Console.Clear();
    }
}

void WriteMenuOption(string number, string title, string? description = null)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"  {number,2}  ");
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write(title);
    if (description is not null)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"  — {description}");
    }

    Console.ResetColor();
    Console.WriteLine();
}

void WriteLabel(string label, string value)
{
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.Write($"\n  {label}: ");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(value);
    Console.ResetColor();
}

void WriteError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n  {message}");
    Console.ResetColor();
}

string WrapText(string text, int width)
{
    var words = text.Split(
        [' ', '\t', '\r', '\n'],
        StringSplitOptions.RemoveEmptyEntries);
    var lines = new List<string>();
    var currentLine = string.Empty;

    foreach (var word in words)
    {
        if (currentLine.Length > 0 && currentLine.Length + word.Length + 1 > width)
        {
            lines.Add(currentLine);
            currentLine = string.Empty;
        }

        currentLine = currentLine.Length == 0 ? word : $"{currentLine} {word}";
    }

    if (currentLine.Length > 0)
    {
        lines.Add(currentLine);
    }

    return string.Join(Environment.NewLine, lines);
}

void PauseForMenu()
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("Prima Enter para voltar ao menu...");
    Console.ResetColor();
}

internal sealed record SavedItem(string Title, string Content, string Source);

internal sealed class BibleResponse
{
    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

internal sealed class RandomVerseResponse
{
    [JsonPropertyName("random_verse")]
    public RandomVerse? RandomVerse { get; init; }
}

internal sealed class RandomVerse
{
    [JsonPropertyName("book")]
    public string? Book { get; init; }

    [JsonPropertyName("chapter")]
    public int Chapter { get; init; }

    [JsonPropertyName("verse")]
    public int Verse { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

internal sealed class BibleIndexResponse
{
    [JsonPropertyName("books")]
    public List<BibleBook>? Books { get; init; }
}

internal sealed class BibleBook
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

internal sealed class BibleBookResponse
{
    [JsonPropertyName("chapters")]
    public List<BibleBookChapter>? Chapters { get; init; }
}

internal sealed class BibleBookChapter
{
    [JsonPropertyName("chapter")]
    public int Chapter { get; init; }
}

internal sealed class BibleChapterResponse
{
    [JsonPropertyName("verses")]
    public List<BibleVerse>? Verses { get; init; }
}

internal sealed class BibleVerse
{
    [JsonPropertyName("verse")]
    public int Verse { get; init; }

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}

internal sealed class JokeResponse
{
    [JsonPropertyName("error")]
    public bool Error { get; init; }

    [JsonPropertyName("setup")]
    public string? Setup { get; init; }

    [JsonPropertyName("delivery")]
    public string? Delivery { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("lang")]
    public string? Language { get; init; }
}
