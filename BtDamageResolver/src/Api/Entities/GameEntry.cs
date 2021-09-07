using System;

namespace Faemiyah.BtDamageResolver.Api.Entities
{
    [Serializable]
    public class GameEntry : NamedEntity
    {
        public DateTime TimeStamp { get; set; }

        public int Players { get; set; }
        
        protected override void EntitySpecificValidate(EntityValidationResult validationResult)
        {
            if (Players <= 0)
            {
                validationResult.Disqualify("A game must have players to be recorded into the repository.");
            }
        }
    }
}