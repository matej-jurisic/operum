namespace Operum.Service.Integrations
{
    /// <summary>
    /// Resolves providers by key, so callers depend on this instead of on any provider.
    /// </summary>
    public interface IIntegrationProviderRegistry
    {
        IReadOnlyList<IIntegrationProvider> All { get; }

        /// <summary>Null when no provider claims the key -- a stale saved connection, say.</summary>
        IIntegrationProvider? Get(string key);

        /// <summary>Null when the key is unknown or its provider cannot be pulled from.</summary>
        IPullIntegrationProvider? GetPull(string key);

        /// <summary>Null when the key is unknown or its provider receives no webhooks.</summary>
        IPushIntegrationProvider? GetPush(string key);
    }

    public class IntegrationProviderRegistry : IIntegrationProviderRegistry
    {
        private readonly Dictionary<string, IIntegrationProvider> _byKey;

        public IntegrationProviderRegistry(IEnumerable<IIntegrationProvider> providers)
        {
            _byKey = new Dictionary<string, IIntegrationProvider>(StringComparer.OrdinalIgnoreCase);

            foreach (var provider in providers)
            {
                // Two providers under one key would make which one runs depend on registration
                // order, and the loser's stored connections would silently start syncing from
                // somewhere else. Fail at startup instead.
                if (!_byKey.TryAdd(provider.Key, provider))
                    throw new InvalidOperationException($"More than one integration provider is registered under the key '{provider.Key}'.");
            }

            All = [.. _byKey.Values];
        }

        public IReadOnlyList<IIntegrationProvider> All { get; }

        public IIntegrationProvider? Get(string key) =>
            _byKey.TryGetValue(key, out var provider) ? provider : null;

        public IPullIntegrationProvider? GetPull(string key) => Get(key) as IPullIntegrationProvider;

        public IPushIntegrationProvider? GetPush(string key) => Get(key) as IPushIntegrationProvider;
    }
}
