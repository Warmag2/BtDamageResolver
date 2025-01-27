using System.Collections.Generic;

namespace Faemiyah.BtDamageResolver.Api.Validation;

/// <summary>
/// Result of object validation.
/// </summary>
public class RulesValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RulesValidationResult"/> class.
    /// </summary>
    public RulesValidationResult()
    {
        IsValid = true;
        Reasons = new List<string>();
    }

    /// <summary>
    /// Is the object valid according to game rules.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Disqualification reasons, if any.
    /// </summary>
    public List<string> Reasons { get; set; }

    /// <summary>
    /// Fail validation with a reason.
    /// </summary>
    /// <param name="reason">The reason to fail with.</param>
    public void Fail(string reason)
    {
        IsValid = false;
        Reasons.Add(reason);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Join(" - ", Reasons);
    }
}