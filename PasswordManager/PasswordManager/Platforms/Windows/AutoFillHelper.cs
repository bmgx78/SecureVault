// Windows credential integration
// On Windows, SecureVault integrates with the browser extension via a Native Messaging host.
// The browser extension communicates with this process via stdin/stdout JSON messages.
//
// To register as a native messaging host:
// HKEY_CURRENT_USER\SOFTWARE\Google\Chrome\NativeMessagingHosts\com.securevault.host
// pointing to the path of the compiled exe.

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PasswordManager.Platforms.Windows
{
    public static class NativeMessagingHost
    {
        public static async Task RunAsync()
        {
            while (true)
            {
                // Read 4-byte length prefix (little-endian)
                byte[] lenBuf = new byte[4];
                int read = await Console.OpenStandardInput().ReadAsync(lenBuf, 0, 4);
                if (read < 4) break;

                int length = BitConverter.ToInt32(lenBuf, 0);
                byte[] msgBuf = new byte[length];
                await Console.OpenStandardInput().ReadAsync(msgBuf, 0, length);
                string message = Encoding.UTF8.GetString(msgBuf);

                var request = JsonSerializer.Deserialize<NativeMessage>(message);
                string response = await HandleRequestAsync(request!);
                await WriteResponseAsync(response);
            }
        }

        private static async Task<string> HandleRequestAsync(NativeMessage msg)
        {
            return msg.Action switch
            {
                "get_credentials" => await GetCredentialsAsync(msg.Domain ?? string.Empty),
                "fill_credentials" => JsonSerializer.Serialize(new { ok = true }),
                "generate_password" => JsonSerializer.Serialize(new
                {
                    password = Services.EncryptionService.GeneratePassword()
                }),
                _ => JsonSerializer.Serialize(new { error = "Unknown action" })
            };
        }

        private static async Task<string> GetCredentialsAsync(string domain)
        {
            // In production: load vault, decrypt, filter by domain
            // Returns list of {title, username} (never sends plaintext passwords over messaging)
            await Task.CompletedTask;
            return JsonSerializer.Serialize(new { credentials = Array.Empty<object>() });
        }

        private static async Task WriteResponseAsync(string response)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(response);
            byte[] lenBuf = BitConverter.GetBytes(bytes.Length);
            var stdout = Console.OpenStandardOutput();
            await stdout.WriteAsync(lenBuf, 0, 4);
            await stdout.WriteAsync(bytes, 0, bytes.Length);
            await stdout.FlushAsync();
        }

        private record NativeMessage(string Action, string? Domain, string? Id);
    }
}
