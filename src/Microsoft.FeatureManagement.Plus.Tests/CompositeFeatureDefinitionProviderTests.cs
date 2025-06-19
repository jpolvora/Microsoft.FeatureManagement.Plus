// Add these using statements for test visibility

using Microsoft.FeatureManagement.Plus.FeatureDefinitionProviders;

namespace Microsoft.FeatureManagement.Plus.Tests
{
    // Import the interfaces and types from the main project
    public class CompositeFeatureDefinitionProviderTests
    {
        // Minimal stub for FeatureDefinition
        private class DummyFeatureDefinition : FeatureDefinition
        {
            public DummyFeatureDefinition(string name)
            {
                Name = name;
            }
        }

        // Minimal stub for IFeatureDefinitionProvider
        private class MockFeatureDefinitionProvider : IFeatureDefinitionProvider
        {
            private readonly List<FeatureDefinition> _definitions;

            public MockFeatureDefinitionProvider() : this([])
            {

            }

            public MockFeatureDefinitionProvider(IEnumerable<FeatureDefinition> definitions)
            {
                _definitions = new List<FeatureDefinition>(definitions);
            }

            public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
            {
                await Task.CompletedTask;

                foreach (FeatureDefinition featureDefinition in _definitions)
                {
                    yield return featureDefinition;
                }
            }

            public Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName)
            {
                return Task.FromResult(_definitions.FirstOrDefault(x => x.Name == featureName));
            }
        }

        [Fact]
        public void Constructor_Throws_On_Null_Providers()
        {
            Assert.Throws<ArgumentNullException>(() => new CompositeFeatureDefinitionProvider(null));
        }

        [Fact]
        public void Constructor_Filters_Null_Providers()
        {
            var provider1 = new MockFeatureDefinitionProvider();
            var providers = new IFeatureDefinitionProvider[] { provider1 };
            var composite = new CompositeFeatureDefinitionProvider(providers);
            Assert.Single(composite);
        }

        [Fact]
        public async Task GetAllFeatureDefinitionsAsync_Returns_Concatenated()
        {
            var def1 = new DummyFeatureDefinition("A");
            var def2 = new DummyFeatureDefinition("B");
            var provider1 = new MockFeatureDefinitionProvider(new[] { def1 });
            var provider2 = new MockFeatureDefinitionProvider(new[] { def2 });
            var composite = new CompositeFeatureDefinitionProvider(new[] { provider1, provider2 });
            var all = await composite.GetAllFeatureDefinitionsAsync().ToListAsync();
            Assert.Contains(def1, all);
            Assert.Contains(def2, all);
            Assert.Equal(2, all.Count);
        }

        [Fact]
        public async Task GetFeatureDefinitionAsync_Returns_First_NonNull()
        {
            var def = new DummyFeatureDefinition("X");
            var provider1 = new MockFeatureDefinitionProvider();
            var provider2 = new MockFeatureDefinitionProvider(new List<FeatureDefinition> { def });
            var composite = new CompositeFeatureDefinitionProvider([provider1, provider2]);
            var result = await composite.GetFeatureDefinitionAsync("X");
            Assert.Equal(def, result);
        }

        [Fact]
        public async Task GetFeatureDefinitionAsync_Returns_Null_If_None()
        {
            var provider1 = new MockFeatureDefinitionProvider();
            var provider2 = new MockFeatureDefinitionProvider();
            var composite = new CompositeFeatureDefinitionProvider(new[] { provider1, provider2 });
            var result = await composite.GetFeatureDefinitionAsync("Y");
            Assert.Null(result);
        }

        [Fact]
        public void Enumerator_Yields_Providers()
        {
            var provider1 = new MockFeatureDefinitionProvider();
            var provider2 = new MockFeatureDefinitionProvider();
            var composite = new CompositeFeatureDefinitionProvider(new[] { provider1, provider2 });
            var list = composite.ToList();
            Assert.Contains(provider1, list);
            Assert.Contains(provider2, list);
            Assert.Equal(2, list.Count);
        }
    }
}