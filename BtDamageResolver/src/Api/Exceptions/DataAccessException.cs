using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Faemiyah.BtDamageResolver.Api.Enums;

namespace Faemiyah.BtDamageResolver.Api.Exceptions;

/// <summary>
/// Represents errors which occur during data access operations.
/// </summary>
[Serializable]
public class DataAccessException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataAccessException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code for this exception.</param>
    public DataAccessException(DataAccessErrorCode errorCode)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataAccessException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code for this exception.</param>
    /// <param name="errorMessage">The error message.</param>
    public DataAccessException(DataAccessErrorCode errorCode, string errorMessage) : base(errorMessage)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataAccessException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code for this exception.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DataAccessException(DataAccessErrorCode errorCode, string errorMessage, Exception innerException) : base(errorMessage, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataAccessException"/> class.
    /// </summary>
    /// <param name="serializationInfo">The serialization info.</param>
    /// <param name="streamingContext">The streaming context.</param>
    [Obsolete("This overload of ISerializable constructor is no longer in use.")]
    [SuppressMessage("Info Code Smell", "S1133:Deprecated code should be removed", Justification = "This is the only way to extend an Exception at the moment.")]
    protected DataAccessException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
        ErrorCode = Enum.Parse<DataAccessErrorCode>(serializationInfo.GetString(nameof(ErrorCode)));
    }

    /// <summary>
    /// The error code associated with this exception.
    /// </summary>
    public DataAccessErrorCode ErrorCode { get; set; }

    /// <inheritdoc/>
    [Obsolete("This implementation of ISerializable is no longer in use.")]
    [SuppressMessage("Info Code Smell", "S1133:Deprecated code should be removed", Justification = "This is the only way to extend an Exception at the moment.")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
    }
}