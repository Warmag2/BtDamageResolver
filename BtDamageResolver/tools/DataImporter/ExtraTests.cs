using System.Collections.Generic;
using Faemiyah.BtDamageResolver.Api.Entities;
using Faemiyah.BtDamageResolver.Api.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Faemiyah.BtDamageResolver.Tools.DataImporter
{
    public static class ExtraTests
    {
        public static void PaperDollTests(ILogger logger)
        {
            var paperDoll = new PaperDoll
            {
                Direction = Direction.Front,
                AttackType = AttackType.Normal,
                Type = PaperDollType.Mech,
                LocationMapping = new Dictionary<int, List<Location>>
                    {
                        { 2, new List<Location> {Location.CenterTorso} },
                        { 3, new List<Location> {Location.RightArm} },
                        { 4, new List<Location> {Location.RightArm} },
                        { 5, new List<Location> {Location.RightLeg} },
                        { 6, new List<Location> {Location.RightTorso} },
                        { 7, new List<Location> {Location.CenterTorso} },
                        { 8, new List<Location> {Location.LeftTorso} },
                        { 9, new List<Location> {Location.LeftLeg} },
                        { 10, new List<Location> {Location.LeftArm} },
                        { 11, new List<Location> {Location.LeftArm} },
                        { 12, new List<Location> {Location.Head} }
                    },
                CriticalDamageMapping = new Dictionary<int, CriticalDamageTableType>
                    {
                        { 2, CriticalDamageTableType.Critical },
                        { 3, CriticalDamageTableType.None },
                        { 4, CriticalDamageTableType.None },
                        { 5, CriticalDamageTableType.None },
                        { 6, CriticalDamageTableType.None },
                        { 7, CriticalDamageTableType.None },
                        { 8, CriticalDamageTableType.None },
                        { 9, CriticalDamageTableType.None },
                        { 10, CriticalDamageTableType.None },
                        { 11, CriticalDamageTableType.None },
                        { 12, CriticalDamageTableType.None }
                    }
            };

            var objectString = @"{
                ""UnitType"": ""Mech"",
                ""Type"": ""Ranged"",
                ""Direction"": ""Front"",
                ""LocationMapping"": {
                    ""2"": [ ""CenterTorso"" ],
                    ""3"": [ ""RightArm"" ],
                    ""4"": [ ""RightArm"" ],
                    ""5"": [ ""RightLeg"" ],
                    ""6"": [ ""RightTorso"" ],
                    ""7"": [ ""CenterTorso"" ],
                    ""8"": [ ""LeftTorso"" ],
                    ""9"": [ ""LeftLeg"" ],
                    ""10"": [ ""LeftArm"" ],
                    ""11"": [ ""LeftArm"" ],
                    ""12"": [ ""Head"" ]
                },
                ""CriticalDamageMapping"": {
                    ""2"": ""Critical"",
                    ""3"": ""None"",
                    ""4"": ""None"",
                    ""5"": ""None"",
                    ""6"": ""None"",
                    ""7"": ""None"",
                    ""8"": ""None"",
                    ""9"": ""None"",
                    ""10"": ""None"",
                    ""11"": ""None"",
                    ""12"": ""None""
                }
            }";

            var paperDollString = JsonConvert.SerializeObject(paperDoll);
            var paperDollObject = JsonConvert.DeserializeObject<PaperDoll>(objectString);

            logger.LogInformation(JsonConvert.SerializeObject(paperDoll));
        }
    }
}