using System;

namespace Faemiyah.BtDamageResolver.Api.Enums
{
    /// <summary>
    /// A data access error code.
    /// </summary>
    [Serializable]
    public enum DataAccessErrorCode
    {
        /// <summary>
        /// An entity with this key already exists in the repository.
        /// </summary>
        AlreadyExists = 1000,

        /// <summary>
        /// One or more of the properties of this entity conflict with entities already in the repository.
        /// </summary>
        Conflict = 1001,

        /// <summary>
        /// One or more of the property values in this entity are invalid.
        /// </summary>
        InvalidValue = 1002,

        /// <summary>
        /// The repository has encountered an external failure.
        /// </summary>
        OperationFailure = 1003,

        /// <summary>
        /// The requested entity could not be found.
        /// </summary>
        NotFound = 1004
    }
}