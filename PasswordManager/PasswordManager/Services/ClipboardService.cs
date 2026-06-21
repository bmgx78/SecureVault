using System.Threading.Tasks;

namespace PasswordManager.Services
{
    public static class ClipboardService
    {
        private static System.Threading.CancellationTokenSource? _clearCts;

        public static async Task CopyAndAutoClearAsync(string text, int clearAfterSeconds = 30)
        {
            await Clipboard.Default.SetTextAsync(text);

            _clearCts?.Cancel();
            _clearCts = new System.Threading.CancellationTokenSource();
            var token = _clearCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(clearAfterSeconds * 1000, token);
                    string? current = await Clipboard.Default.GetTextAsync();
                    if (current == text)
                        await Clipboard.Default.SetTextAsync(string.Empty);
                }
                catch (System.Threading.Tasks.TaskCanceledException) { }
            });
        }
    }
}
