using Faemiyah.BtDamageResolver.Api.Validation;

namespace Faemiyah.BtDamageResolver.Api.Entities.Interfaces;

/// <summary>
/// Object with rules validation.
/// </summary>
public interface IEntityWithRulesValidation
{
    /// <summary>
    /// Validate an object and return a rules validation result.
    /// </summary>
    /// <returns>The rules validation result.</returns>
    RulesValidationResult Validate();
}