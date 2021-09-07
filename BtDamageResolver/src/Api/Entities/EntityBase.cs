using System;
using Faemiyah.BtDamageResolver.Api.Entities.Interfaces;
using Newtonsoft.Json;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    /// <inheritdoc />
    [Serializable]
    public abstract class EntityBase<TKey> : IEntity<TKey> where TKey : IComparable
    {
        /// <inheritdoc />
        public abstract TKey GetId();

        /// <inheritdoc />
        public abstract void SetId(TKey id);

        /// <inheritdoc />
        public abstract EntityValidationResult Validate();

        /// <summary>
        /// Perform entity-specific validation tasks.
        /// </summary>
        /// <param name="validationResult">The validation result to modify.</param>
        protected abstract void EntitySpecificValidate(EntityValidationResult validationResult);

        /// <inheritdoc />
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}