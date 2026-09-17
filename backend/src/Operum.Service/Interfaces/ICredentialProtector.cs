namespace Operum.Service.Interfaces
{
    // Backed by ASP.NET Core Data Protection; if the key ring is lost, every stored credential
    // becomes undecryptable and every connection must be remade.
    public interface ICredentialProtector
    {
        string Protect(string plaintext);

        // Null when the ciphertext can't be read (tampered, or key ring gone); callers treat that as needing reconnection.
        string? Unprotect(string? ciphertext);

        // The raw value must never reach a client.
        string Mask(string? ciphertext);
    }
}
