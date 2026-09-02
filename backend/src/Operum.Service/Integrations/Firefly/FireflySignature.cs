using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;

namespace Operum.Service.Integrations.Firefly
{
    /// <summary>
    /// Firefly III signs a delivery the way Stripe does: a <c>Signature</c> header carrying a
    /// timestamp and a versioned digest, over "&lt;timestamp&gt;.&lt;raw body&gt;".
    /// <para>
    /// The digest is HMAC-SHA3-256. .NET has <c>HMACSHA3_256</c>, but it is a thin wrapper over
    /// the platform's crypto library and reports <c>IsSupported == false</c> on Windows before
    /// 24H2 -- so relying on it would mean signature checks that pass in a Linux container and
    /// throw on a developer's machine. BouncyCastle is used instead: one implementation, the
    /// same everywhere. It costs nothing to depend on, since WebPush already brings it in; the
    /// package is now referenced explicitly rather than relied on transitively.
    /// </para>
    /// </summary>
    public static class FireflySignature
    {
        public const string HeaderName = "Signature";

        /// <summary>
        /// How far out of date a delivery's timestamp may be. Bounds how long a captured
        /// request stays replayable.
        /// </summary>
        public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

        public enum Outcome { Valid, Malformed, Expired, Mismatch }

        public static Outcome Verify(string? header, string rawBody, string secret, DateTime utcNow)
        {
            if (!TryParse(header, out var timestamp, out var providedHex))
                return Outcome.Malformed;

            var signedAt = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
            if (Math.Abs((utcNow - signedAt).TotalSeconds) > MaxAge.TotalSeconds)
                return Outcome.Expired;

            var expected = Compute(timestamp, rawBody, secret);

            // Fixed-time comparison so a wrong signature cannot be refined a byte at a time.
            var provided = FromHex(providedHex);
            if (provided == null || provided.Length != expected.Length)
                return Outcome.Mismatch;

            return CryptographicOperations.FixedTimeEquals(provided, expected)
                ? Outcome.Valid
                : Outcome.Mismatch;
        }

        /// <summary>HMAC-SHA3-256 over "&lt;timestamp&gt;.&lt;raw body&gt;".</summary>
        public static byte[] Compute(long timestamp, string rawBody, string secret)
        {
            var payload = Encoding.UTF8.GetBytes($"{timestamp}.{rawBody}");

            var hmac = new HMac(new Sha3Digest(256));
            hmac.Init(new KeyParameter(Encoding.UTF8.GetBytes(secret)));
            hmac.BlockUpdate(payload, 0, payload.Length);

            var digest = new byte[hmac.GetMacSize()];
            hmac.DoFinal(digest, 0);
            return digest;
        }

        public static string ComputeHex(long timestamp, string rawBody, string secret) =>
            Convert.ToHexString(Compute(timestamp, rawBody, secret)).ToLowerInvariant();

        /// <summary>Parses "t=1610738765,v1=d62463af…"; unknown parts are ignored.</summary>
        private static bool TryParse(string? header, out long timestamp, out string signature)
        {
            timestamp = 0;
            signature = string.Empty;

            if (string.IsNullOrWhiteSpace(header))
                return false;

            foreach (var part in header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = part.IndexOf('=');
                if (separator <= 0)
                    continue;

                var name = part[..separator];
                var value = part[(separator + 1)..];

                if (name == "t")
                    long.TryParse(value, out timestamp);
                // v1 is the only live scheme; a future v2 would be added here rather than
                // replacing this, so old senders keep working.
                else if (name == "v1")
                    signature = value;
            }

            return timestamp > 0 && signature.Length > 0;
        }

        private static byte[]? FromHex(string value)
        {
            try
            {
                return Convert.FromHexString(value);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
