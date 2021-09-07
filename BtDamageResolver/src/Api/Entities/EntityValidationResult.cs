using System;
using System.Collections.Generic;
using System.Linq;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <summary>
    /// Class containing the result of entity validation.
    /// </summary>
    [Serializable]
    public class EntityValidationResult
    {
        /// <summary>
        /// Empty constructor for EntityValidationResult.
        /// </summary>
        public EntityValidationResult()
        {
            // By default, the result is valid
            IsValid = true;
            DisqualificationReasons = new List<string>();
        }

        /// <summary>
        /// Disqualify this <see cref="EntityValidationResult"/>.
        /// </summary>
        /// <param name="reason">The reason for disqualification.</param>
        public void Disqualify(string reason)
        {
            IsValid = false;
            DisqualificationReasons.Add(reason);
        }

        /// <summary>
        /// The validation result.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Disqualification reason, if any.
        /// </summary>
        public List<string> DisqualificationReasons { get; }

        /// <summary>
        /// Get a single string which lists all disqualification reasons.
        /// </summary>
        /// <returns>A single string listing all reasons for disqualification.</returns>
        public string GetDisqualificationReasons()
        {
            return DisqualificationReasons.Any() ? string.Join(" AND ", DisqualificationReasons.Select(r => $"\'{r}\'")) : string.Empty;
        }
    }
}
