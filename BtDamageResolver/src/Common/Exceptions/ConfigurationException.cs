using System;
using System.Runtime.Serialization;

namespace Faemiyah.BtDamageResolver.Common.Exceptions;

/// <summary>
/// Represents errors which occur during configuration operations.
/// </summary>
[Serializable]
public class ConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public ConfigurationException(string errorMessage) : base(errorMessage)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConfigurationException(string errorMessage, Exception innerException) : base(errorMessage, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    /// <param name="serializationInfo">The serialization info.</param>
    /// <param name="streamingContext">The streaming context.</param>
    protected ConfigurationException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
    }
}