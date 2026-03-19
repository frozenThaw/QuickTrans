using QuickTrans.App.Models;

namespace QuickTrans.App.Services;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken);
}
