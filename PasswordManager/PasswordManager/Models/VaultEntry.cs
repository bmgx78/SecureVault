using System;
using System.Collections.Generic;

namespace PasswordManager.Models
{
    public class VaultEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Category { get; set; } = "Login";
        public bool IsFavorite { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsed { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? Totp { get; set; }
        public bool HasBreachAlert { get; set; } = false;
        public int PasswordStrength { get; set; } = 0;

        // Custom fields (e.g. for credit cards, secure notes)
        public Dictionary<string, string> CustomFields { get; set; } = new();
    }

    public class VaultEntryCategory
    {
        public const string Login = "Login";
        public const string CreditCard = "CreditCard";
        public const string Identity = "Identity";
        public const string SecureNote = "SecureNote";
        public const string SSHKey = "SSHKey";
        public const string ApiKey = "ApiKey";
    }
}
