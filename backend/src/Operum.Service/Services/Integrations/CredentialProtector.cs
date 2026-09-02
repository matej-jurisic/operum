using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Operum.Service.Interfaces;

namespace Operum.Service.Services.Integrations
{
    public class CredentialProtector : ICredentialProtector
    {
        // Namespacing the protector means a credential ciphertext cannot be decrypted by any
        // other purpose in the app, even with the same key ring.
        private const string Purpose = "Operum.Integrations.Credentials";

        private readonly IDataProtector _protector;
        private readonly ILogger<CredentialProtector> _logger;

        public CredentialProtector(IDataProtectionProvider provider, ILogger<CredentialProtector> logger)
        {
            _protector = provider.CreateProtector(Purpose);
            _logger = logger;
        }

        public string Protect(string plaintext) => _protector.Protect(plaintext);

        public string? Unprotect(string? ciphertext)
        {
            if (string.IsNullOrWhiteSpace(ciphertext))
                return null;

            try
            {
                return _protector.Unprotect(ciphertext);
            }
            catch (Exception ex)
            {
                // Nearly always a key ring that did not survive a restart. Logged without the
                // ciphertext, and reported to the user as a connection to remake.
                _logger.LogError(ex, "Could not decrypt a stored integration credential");
                return null;
            }
        }

        public string Mask(string? ciphertext)
        {
            var plaintext = Unprotect(ciphertext);
            if (string.IsNullOrEmpty(plaintext))
                return string.Empty;

            return plaintext.Length <= 4
                ? new string('•', plaintext.Length)
                : $"…{plaintext[^4..]}";
        }
    }
}
