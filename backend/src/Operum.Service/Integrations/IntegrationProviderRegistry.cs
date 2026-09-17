namespace Operum.Service.Integrations
{
    public interface IIntegrationProviderRegistry
    {
        IReadOnlyList<IIntegrationProvider> All { get; }

        IIntegrationProvider? Get(string key);

        IPullIntegrationProvider? GetPull(string key);

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
                // Fail at startup rather than let two providers under one key race on registration order.
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
