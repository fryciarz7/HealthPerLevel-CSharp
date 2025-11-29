using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HealthPerLevel_cs.config;
using HealthPerLevel_cs.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.Profile;
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

        private bool isOnLoad = false;

        public HealthPerLevel(SaveServer saveServer, ModHelper modHelper, ISptLogger<HealthPerLevel> logger)
        {
            _saveServer = saveServer;
            _modHelper = modHelper;
            _logger = logger;

            string? pathToMod = _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            _config = _modHelper.GetJsonDataFromFile<ConfigJson>(pathToMod, "config/config.json");
        }

        public Task DoStuff(bool _isOnLoad)
        {
            isOnLoad = _isOnLoad;
            if (_config.enabled)
            {
                HpChanges();
            }
            return Task.CompletedTask;
        }

        private void HpChanges()
        {
            try
            {
                var profiles = _saveServer.GetProfiles();

                foreach (var kvp in profiles)
                {
                    SptProfile? profile = kvp.Value;

                    if (profile?.CharacterData?.PmcData != null)
                    {
                        //_logger.Info($"{LogPrefix}Modify PMC for {kvp.Key}");
                        CalculateCharacterData(profile.CharacterData.PmcData, _config.PMC);
                    }
                    if (profile?.CharacterData?.ScavData != null)
                    {
                        //_logger.Info($"{LogPrefix}Modify SCAV for {kvp.Key}");
                        CalculateCharacterData(profile.CharacterData.ScavData, _config.SCAV);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogPrefix}Error: {ex.Message}");
            }
        }

        private void CalculateCharacterData<T, E>(PmcData character, ICharacter<T, E> charType)
        {
            double? accLv = CheckLevelCap(character, charType);// character.Info?.Level;
            //_logger.Info($"{LogPrefix}accLv: {accLv.Value}");
            //_logger.Info($"{LogPrefix}GetPmcIncrement(accLv.Value) {GetPmcIncrement(accLv.Value)}");
            foreach (var (bodyPartName, bodyPart) in character.Health.BodyParts)
            {
                if (bodyPart != null && bodyPart.Health != null)
                {
                    ModifyHealth(accLv.Value, charType, bodyPartName, bodyPart);
                }
            }
        }

        private int CheckLevelCap<T, E>(PmcData character, ICharacter<T, E> charType)
        {
            return charType.level_cap ? Math.Min(character.Info.Level.Value, charType.level_cap_value) : character.Info.Level.Value;
        }

        private void ModifyHealth<T, E>(double accLv, ICharacter<T, E> charType, string bodyPartName, BodyPartHealth bodyPart)
        {
            IBaseHealth baseHealth = charType.base_health as IBaseHealth;
            IIncreasePerLevel increaseHealth = charType.increase_per_level as IIncreasePerLevel;
            switch (bodyPartName)
            {
                case "Head":
                    bodyPart.Health.Maximum =
                        baseHealth.head_base_health + (GetIncrement(accLv, charType) * increaseHealth.head_health_per_level);
                    break;

                case "Chest":
                    bodyPart.Health.Maximum =
                        baseHealth.thorax_base_health + (GetIncrement(accLv, charType) * increaseHealth.thorax_health_per_level);
                    break;

                case "Stomach":
                    bodyPart.Health.Maximum =
                        baseHealth.stomach_base_health + (GetIncrement(accLv, charType) * increaseHealth.stomach_health_per_level);
                    break;

                case "LeftArm":
                    bodyPart.Health.Maximum =
                        baseHealth.left_arm_base_health + (GetIncrement(accLv, charType) * increaseHealth.left_arm_per_level);
                    break;

                case "LeftLeg":
                    bodyPart.Health.Maximum =
                        baseHealth.left_leg_base_health + (GetIncrement(accLv, charType) * increaseHealth.left_leg_per_level);
                    break;

                case "RightArm":
                    bodyPart.Health.Maximum =
                        baseHealth.right_arm_base_health + (GetIncrement(accLv, charType) * increaseHealth.right_arm_per_level);
                    break;

                case "RightLeg":
                    bodyPart.Health.Maximum =
                        baseHealth.right_leg_base_health + (GetIncrement(accLv, charType) * increaseHealth.right_leg_per_level);
                    break;

                default:
                    _logger.Info($"{bodyPartName} is missing");
                    break;
            }
            //_logger.Info(LogPrefix + baseHealth.GetType().ToString());
            CheckIfTooMuchHealth(bodyPartName, bodyPart);
            ResetScavHealthOnLoad(bodyPart, baseHealth);

            _logger.Success($"{LogPrefix}Modified {bodyPartName} to {bodyPart.Health.Maximum}");
        }

        private void ResetScavHealthOnLoad(BodyPartHealth bodyPart, IBaseHealth baseHealth)
        {
            if (baseHealth is Base_Health_SCAV && isOnLoad)
            {
                //_logger.Info($"{LogPrefix}Resetting {baseHealth.GetType().ToString()} {key} health... isOnLoad: {isOnLoad}");
                bodyPart.Health.Current = bodyPart.Health.Maximum;
            }
        }

        private void CheckIfTooMuchHealth(string bodyPartName, BodyPartHealth bodyPart)
        {
            if (bodyPart.Health.Current > bodyPart.Health.Maximum)
            {
                _logger.Warning($"{LogPrefix}How does your {bodyPartName} has more health than max? ({bodyPart.Health.Current}/{bodyPart.Health.Maximum}) Let me fix it...");
                bodyPart.Health.Current = bodyPart.Health.Maximum;
            }
        }

        private double GetIncrement<T, E>(double accountLevel, ICharacter<T, E> charType)
        {
            return Math.Truncate((accountLevel) / (double)charType.levels_per_increment);
        }
    }
}