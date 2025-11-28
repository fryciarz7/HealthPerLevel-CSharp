using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Fence;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Json;

namespace HealthPerLevel_cs
{
    [Injectable]
    public class HealthPerLevel
    {
        private readonly SaveServer _saveServer;
        private readonly DatabaseServer _databaseServer;
        private readonly FenceService _fenceService;
        private readonly ConfigServer _configServer;
        private readonly ISptLogger<HealthPerLevel> _logger;

        public HealthPerLevel(SaveServer saveServer, DatabaseServer databaseServer, ConfigServer configServer, FenceService fenceService, ISptLogger<HealthPerLevel> logger)
        {
            _saveServer = saveServer;
            _databaseServer = databaseServer;
            _configServer = configServer;
            _fenceService = fenceService;
            _logger = logger;

            //LoadConfig();
        }

        public Task DoStuff()
        {
            HpChanges();
            return Task.CompletedTask;
        }

        private void HpChanges()
        {
            try
            {
                var profiles = _saveServer.GetProfiles();
                int modifiedCount = 0;

                _logger.Info(profiles.Count.ToString());

                foreach (var kvp in profiles)
                {
                    var profile = kvp.Value;
                    var pmc = profile?.CharacterData?.PmcData;
                    if (pmc == null)
                        continue;

                    var exp = pmc?.Info?.Experience ?? 0;
                    _logger.Info($"{kvp.Key} {exp}");
                    int bonusHp = Math.Min(exp / 30000, 65);
                    int newMax = exp; // Math.Min(35 + bonusHp, 100);

                    if (pmc.Health?.BodyParts == null || !pmc.Health.BodyParts.ContainsKey("Head"))
                        continue;

                    var head = pmc.Health.BodyParts["Head"];

                    head.Health.Maximum = newMax;
                    if (head.Health.Current > newMax)
                        head.Health.Current = newMax;
                    Console.WriteLine($"\x1b[36m🎮 [Jiang Hu] Increased {pmc.Info.Nickname}'s Head HP to {newMax} (+{newMax - 35})    头变大啦\x1b[0m");
                    modifiedCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\x1b[36m❌ [Jiang Hu] Error adjusting head HP: {ex.Message}  \x1b[0m");
            }
        }
    }
}