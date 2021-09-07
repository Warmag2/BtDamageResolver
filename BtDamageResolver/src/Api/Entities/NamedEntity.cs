namespace Faemiyah.BtDamageResolver.Api.Entities
{
    public abstract class NamedEntity : EntityBase<string>
    {
        /// <summary>
        /// The name of the entity.
        /// </summary>
        public string Name { get; set; }

        /// <inheritdoc />
        public override string GetId() => Name;

        /// <inheritdoc />
        public override void SetId(string id)
        {
            Name = id;
        }

        /// <inheritdoc />
        public override EntityValidationResult Validate()
        {
            var validationResult = new EntityValidationResult();

            // Customer name must be a non-empty non-null string
            if (string.IsNullOrWhiteSpace(Name))
            {
                validationResult.Disqualify("The entity name must be a non-null, non-whitespace string.");
            }

            EntitySpecificValidate(validationResult);

            return validationResult;
        }
    }
}