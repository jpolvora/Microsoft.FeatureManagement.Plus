namespace FeatureManagement
{
    public interface ICacheManager
    {
        /// <summary>
        /// Expires all cache items.
        /// </summary>
        /// <returns></returns>
        void Expire();
    }
}