using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using QuickTrans.App.Models;

namespace QuickTrans.App.Services;

public sealed class GoogleTranslateService : ITranslationService
{
    private readonly HttpClient _httpClient;

    public GoogleTranslateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return TranslationResult.Failure(string.Empty, "Please enter text to translate.");
        }

        var normalizedText = text.Trim();
        var requestUri = BuildRequestUri(normalizedText, ResolveTargetLanguage(normalizedText));

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            var translatedText = ExtractTranslatedText(document.RootElement);

            return string.IsNullOrWhiteSpace(translatedText)
                ? TranslationResult.Failure(normalizedText, "Translation returned an empty response.")
                : TranslationResult.Success(normalizedText, translatedText);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return TranslationResult.Failure(normalizedText, "Unable to reach the translation service.");
        }
        catch (JsonException)
        {
            return TranslationResult.Failure(normalizedText, "Received an unexpected response from the translation service.");
        }
    }

    private static Uri BuildRequestUri(string text, string targetLanguage)
    {
        var escapedText = WebUtility.UrlEncode(text);
        var query = $"client=gtx&sl=auto&tl={targetLanguage}&dt=t&q={escapedText}";
        return new Uri($"https://translate.googleapis.com/translate_a/single?{query}", UriKind.Absolute);
    }

    private static string ResolveTargetLanguage(string text)
    {
        var firstCharacter = text[0];

        if (IsEnglishLetter(firstCharacter))
        {
            return "zh-CN";
        }

        if (IsChineseCharacter(firstCharacter))
        {
            return "en";
        }

        return "en";
    }

    private static bool IsEnglishLetter(char value)
    {
        return value is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }

    private static bool IsChineseCharacter(char value)
    {
        return value is >= '\u3400' and <= '\u4DBF'
            or >= '\u4E00' and <= '\u9FFF'
            or >= '\uF900' and <= '\uFAFF';
    }

    private static string ExtractTranslatedText(JsonElement rootElement)
    {
        if (rootElement.ValueKind != JsonValueKind.Array || rootElement.GetArrayLength() == 0)
        {
            throw new JsonException("Unexpected translation payload.");
        }

        var sentencesElement = rootElement[0];

        if (sentencesElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Unexpected sentence payload.");
        }

        var builder = new StringBuilder();

        foreach (var sentenceElement in sentencesElement.EnumerateArray())
        {
            if (sentenceElement.ValueKind != JsonValueKind.Array || sentenceElement.GetArrayLength() == 0)
            {
                continue;
            }

            var translatedPart = sentenceElement[0];

            if (translatedPart.ValueKind == JsonValueKind.String)
            {
                builder.Append(translatedPart.GetString());
            }
        }

        return builder.ToString().Trim();
    }
}
