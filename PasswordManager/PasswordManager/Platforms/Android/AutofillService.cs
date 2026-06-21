using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Service.Autofill;
using Android.Views.Autofill;
using Android.Widget;
using AndroidX.AutoFill.Inline.V1;
using System.Linq;

namespace PasswordManager.Platforms.Android
{
    /// <summary>
    /// Android AutofillService — provides password auto-fill for all apps and browsers.
    /// Declared in AndroidManifest.xml with the BIND_AUTOFILL_SERVICE permission.
    /// </summary>
    [Service(
        Label = "SecureVault AutoFill",
        Permission = "android.permission.BIND_AUTOFILL_SERVICE",
        Exported = true)]
    [IntentFilter(new[] { "android.service.autofill.AutofillService" })]
    [MetaData("android.autofill", Resource = "@xml/autofill_service")]
    public class SecureVaultAutofillService : AutofillService
    {
        public override void OnFillRequest(FillRequest request, CancellationSignal cancellationSignal, FillCallback callback)
        {
            var structure = request.FillContexts?.LastOrDefault()?.Structure;
            if (structure == null)
            {
                callback.OnFailure("No structure.");
                return;
            }

            var (usernameId, passwordId, domain) = ParseStructure(structure);
            if (usernameId == null && passwordId == null)
            {
                callback.OnFailure("No autofill fields found.");
                return;
            }

            // Launch the unlock/picker activity
            var intent = new global::Android.Content.Intent(this, typeof(AutofillPickerActivity));
            intent.PutExtra("domain", domain);
            var pi = global::Android.App.PendingIntent.GetActivity(
                this, 0, intent,
                global::Android.App.PendingIntentFlags.CancelCurrent |
                global::Android.App.PendingIntentFlags.Mutable)!;

            var presentation = BuildPresentation("SecureVault — tap to fill");
            var responseBuilder = new FillResponse.Builder();

            if (usernameId != null || passwordId != null)
            {
                var datasetBuilder = new Dataset.Builder();
                if (usernameId != null)
                    datasetBuilder.SetValue(usernameId, AutofillValue.ForText(""), presentation);
                if (passwordId != null)
                    datasetBuilder.SetValue(passwordId, AutofillValue.ForText(""), presentation);
                datasetBuilder.SetAuthentication(pi);
                responseBuilder.AddDataset(datasetBuilder.Build());
            }

            callback.OnSuccess(responseBuilder.Build());
        }

        public override void OnSaveRequest(SaveRequest request, SaveCallback callback)
        {
            // Prompt user to save new credentials
            var structure = request.FillContexts?.LastOrDefault()?.Structure;
            if (structure == null) { callback.OnFailure("No structure."); return; }
            var intent = new global::Android.Content.Intent(this, typeof(AutofillSaveActivity));
            intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
            StartActivity(intent);
            callback.OnSuccess();
        }

        private RemoteViews BuildPresentation(string text)
        {
            var views = new RemoteViews(PackageName, Resource.Layout.autofill_item);
            views.SetTextViewText(Resource.Id.autofill_text, text);
            return views;
        }

        private static (AutofillId? username, AutofillId? password, string domain) ParseStructure(AssistStructure structure)
        {
            AutofillId? usernameId = null, passwordId = null;
            string domain = string.Empty;

            for (int i = 0; i < structure.WindowNodeCount; i++)
            {
                var node = structure.GetWindowNodeAt(i);
                domain = node.Title?.ToString() ?? string.Empty;
                TraverseNode(node.RootViewNode, ref usernameId, ref passwordId);
            }
            return (usernameId, passwordId, domain);
        }

        private static void TraverseNode(AssistStructure.ViewNode? node,
            ref AutofillId? usernameId, ref AutofillId? passwordId)
        {
            if (node == null) return;

            if (node.AutofillType == (int)AutofillType.Text)
            {
                var hints = node.GetAutofillHints();
                if (hints != null)
                {
                    if (hints.Contains(View.AutofillHintUsername) ||
                        hints.Contains(View.AutofillHintEmailAddress))
                        usernameId = node.AutofillId;
                    if (hints.Contains(View.AutofillHintPassword))
                        passwordId = node.AutofillId;
                }
                else
                {
                    // Heuristic fallback
                    string? hint = node.Hint?.ToLowerInvariant() ?? "";
                    if (hint.Contains("email") || hint.Contains("user") || hint.Contains("login"))
                        usernameId = node.AutofillId;
                    if (hint.Contains("password") || hint.Contains("passwd") || hint.Contains("pwd"))
                        passwordId = node.AutofillId;
                }
            }

            for (int i = 0; i < node.ChildCount; i++)
                TraverseNode(node.GetChildAt(i), ref usernameId, ref passwordId);
        }
    }
}
