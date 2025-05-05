using System;

namespace FeatureManagement
{
    public class FeatureManagementException : Exception
    {
        public FeatureManagementException(string message) : base(message)
        {
        }
        public FeatureManagementException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}