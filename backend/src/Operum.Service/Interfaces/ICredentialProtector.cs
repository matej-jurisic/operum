namespace Operum.Service.Interfaces
{
    /// <summary>
    /// Encrypts integration credentials at rest. Backed by ASP.NET Core Data Protection, so
    /// its keys must outlive the container -- if the key ring is lost, every stored credential
    /// becomes undecryptable and every connection has to be made again.
    /// </summary>
    public interface ICredentialProtector
    {
        string Protect(string plaintext);

        /// <summary>
        /// Null when the ciphertext cannot be read: tampered with, or written under a key ring
        /// that is gone. Callers surface that as a connection needing to be remade, never as
        /// a crash.
        /// </summary>
        string? Unprotect(string? ciphertext);

        /// <summary>
        /// The last few characters, for showing which credential is stored without disclosing
        /// it. The raw value must never reach a client.
        /// </summary>
        string Mask(string? ciphertext);
    }
}
