using System.Net;
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

    public async Task<TranslationResult> TranslateToChineseAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return TranslationResult.Failure(string.Empty, "Please enter text to translate.");
        }

        var requestUri = BuildRequestUri(text.Trim());

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            var translatedText = ExtractTranslatedText(document.RootElement);

            return string.IsNullOrWhiteSpace(translatedText)
                ? TranslationResult.Failure(text, "Translation returned an empty response.")
                : TranslationResult.Success(text, translatedText);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return TranslationResult.Failure(text, "Unable to reach the translation service.");
        }
        catch (JsonException)
        {
            return TranslationResult.Failure(text, "Received an unexpected response from the translation service.");
        }
    }

    private static Uri BuildRequestUri(string text)
    {
        var escapedText = WebUtility.UrlEncode(text);
        var query = $"client=gtx&sl=auto&tl=zh-CN&dt=t&q={escapedText}";
        return new Uri($"https://translate.googleapis.com/translate_a/single?{query}", UriKind.Absolute);
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
