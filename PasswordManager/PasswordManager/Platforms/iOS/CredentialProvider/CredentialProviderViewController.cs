// iOS Credential Provider Extension
// This file belongs in a separate Extension target: PasswordManager.CredentialProviderExtension
// Add a new "Credential Provider Extension" target in Visual Studio, then move this file there.
//
// The main app shares its encrypted vault via an App Group (group.com.securevault.shared).

using AuthenticationServices;
using Foundation;
using UIKit;

namespace PasswordManager.iOS.CredentialProvider
{
    public partial class CredentialProviderViewController : ASCredentialProviderViewController
    {
        public override void PrepareCredentialList(ASCredentialServiceIdentifier[] serviceIdentifiers)
        {
            // Called when the user taps the key icon in Safari / any iOS app
            // Show a filtered list of entries matching serviceIdentifiers[0].Identifier (the domain)
            var domain = serviceIdentifiers.FirstOrDefault()?.Identifier ?? string.Empty;
            ShowPickerUI(domain);
        }

        public override void ProvideCredentialWithoutUserInteraction(ASPasswordCredential credential)
        {
            // Only called if the vault is already unlocked (no master password needed)
            // Return immediately if locked
            ExtensionContext.CancelRequest(new NSError(
                new NSString("com.securevault"), 0,
                new NSDictionary(NSError.LocalizedDescriptionKey,
                    new NSString("Vault is locked. Open SecureVault to unlock."))));
        }

        private void ShowPickerUI(string domain)
        {
            // Load shared vault from App Group container
            var groupContainer = NSFileManager.DefaultManager
                .GetContainerUrl("group.com.securevault.shared");
            var vaultPath = groupContainer?.Append("vault.enc", false).Path;

            if (vaultPath == null || !NSFileManager.DefaultManager.FileExists(vaultPath))
            {
                ExtensionContext.CancelRequest(new NSError(
                    new NSString("com.securevault"), 1,
                    new NSDictionary(NSError.LocalizedDescriptionKey,
                        new NSString("No vault found. Open SecureVault first."))));
                return;
            }

            // Present unlock + picker UI
            // After user authenticates and selects an entry:
            // ExtensionContext.CompleteRequest(
            //     new ASPasswordCredential(username, password), null);
        }
    }
}
