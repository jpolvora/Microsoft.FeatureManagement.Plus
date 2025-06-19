namespace Microsoft.FeatureManagement.Plus.Patterns
{
    public interface IGenericDecorator<T>
    {
        T Target { get; }

    }
}