namespace QuickTrans.App.Models;

public sealed record TranslationResult(
    string SourceText,
    string TranslatedText,
    bool IsSuccess,
    string ErrorMessage)
{
    public static TranslationResult Success(string sourceText, string translatedText)
    {
        return new TranslationResult(sourceText, translatedText, true, string.Empty);
    }

    public static TranslationResult Failure(string sourceText, string errorMessage)
    {
        return new TranslationResult(sourceText, string.Empty, false, errorMessage);
    }
}
