using System;
using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Api.ClientInterface.Events;

/// <summary>
/// Event for sending client error data.
/// </summary>
public class ClientErrorEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientErrorEvent"/> class.
    /// </summary>
    public ClientErrorEvent()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientErrorEvent"/> class.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="invalidUnitIds">A collection of invalid unit IDs, if any.</param>
    public ClientErrorEvent(string errorMessage, HashSet<Guid> invalidUnitIds = null)
    {
        ErrorMessage = errorMessage;
        InvalidUnitIds = invalidUnitIds;
    }

    /// <summary>
    /// The error message.
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// List of unit IDs with errors.
    /// </summary>
    public HashSet<Guid> InvalidUnitIds { get; set; }
}
