using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HealthPerLevel_cs.config;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
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
        private readonly ModHelper _modHelper;
        private readonly ISptLogger<HealthPerLevel> _logger;

        private readonly ConfigJson _config;
        private const string LogPrefix = "[HealthPerLevel] ";

        public HealthPerLevel(SaveServer saveServer, ModHelper modHelper, ISptLogger<HealthPerLevel> logger)
        {
            _saveServer = saveServer;
            _modHelper = modHelper;
            _logger = logger;

            var pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            _config = _modHelper.GetJsonDataFromFile<ConfigJson>(pathToMod, "config/config.json");
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
                    _logger.Info($"{LogPrefix}Increased {pmc.Info.Nickname}'s Head HP to {newMax} (+{newMax - 35})    头变大啦\x1b[0m");
                    modifiedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogPrefix}Error adjusting head HP: {ex.Message}  \x1b[0m");
            }
        }
    }
}