using System;
using System.ComponentModel.DataAnnotations;

namespace Faemiyah.BtDamageResolver.Api.Entities.Prototypes
{
    /// <summary>
    /// A named entity.
    /// </summary>
    [Serializable]
    public abstract class NamedEntity : EntityBase<string>
    {
        /// <summary>
        /// The name of the entity.
        /// </summary>
        [Required]
        [RegularExpression(@"^[A-Za-z0-9_-]*$", ErrorMessage = "Username must be composed of alphanumeric characters, dashes and underscores.")]
        [StringLength(32, ErrorMessage = "{0} length must be between {2} and {1} characters.", MinimumLength = 1)]

        public string Name { get; set; }

        /// <inheritdoc />
        public override string GetId() => Name;

        /// <inheritdoc />
        public override void SetId(string id)
        {
            Name = id;
        }
    }
}