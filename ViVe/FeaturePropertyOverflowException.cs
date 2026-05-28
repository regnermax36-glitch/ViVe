using System;

namespace Albacore.ViVe.Exceptions
{
    public class FeaturePropertyOverflowException : Exception
    {
        public FeaturePropertyOverflowException(string propertyName, uint maxValue)
            : base($"Value for property '{propertyName}' exceeds maximum allowed value ({maxValue}).")
        {
        }
    }
}
