using System;
using Faemiyah.BtDamageResolver.Api.Entities.Prototypes;

namespace Faemiyah.BtDamageResolver.Api.Entities.RepositoryEntities
{
    [Serializable]
    public class GameEntry : NamedEntity
    {
        public DateTime TimeStamp { get; set; }

        public bool PasswordProtected { get; set; }

        public int Players { get; set; }
    }
}