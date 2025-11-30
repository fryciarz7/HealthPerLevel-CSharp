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
                        CalculateCharacterData(profile.CharacterData.PmcData, _config.PMC);
                    }
                    if (profile?.CharacterData?.ScavData != null)
                    {
                        CalculateCharacterData(profile.CharacterData.ScavData, _config.SCAV);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogPrefix}Error: {ex.Message}");
            }
        }

        private void CalculateCharacterData<T, E, G>(PmcData character, ICharacter<T, E, G> charType)
        {
            double? accLv = CheckLevelCap(character, charType);
            double healthSkill = GetHealthLevel(character, charType);
            foreach (var (bodyPartName, bodyPart) in character.Health.BodyParts)
            {
                if (bodyPart != null && bodyPart.Health != null)
                {
                    ModifyHealth(accLv.Value, charType, healthSkill, bodyPartName, bodyPart);
                }
            }
        }

        private void ModifyHealth<T, E, G>(double accLv, ICharacter<T, E, G> charType, double hpSkillv, string bodyPartName, BodyPartHealth bodyPart)
        {
            IBaseHealth baseHealth = charType.base_health as IBaseHealth;
            IIncreasePerLevel increaseHealth = charType.increase_per_level as IIncreasePerLevel;
            IIncreasePerLevel increasePerHealthSkill = charType.increase_per_health_skill_level as IIncreasePerLevel;

            if (charType.health_per_health_skill_level)

                switch (bodyPartName)
                {
                    case "Head":
                        bodyPart.Health.Maximum = AddHpPerLevel(accLv, charType, bodyPart, baseHealth, increaseHealth.head_health_per_level) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.head_health_per_level);
                        break;

                    case "Chest":
                        bodyPart.Health.Maximum = AddHpPerLevel(accLv, charType, bodyPart, baseHealth, increaseHealth.thorax_health_per_level) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.thorax_health_per_level);
                        break;

                    case "Stomach":
                        bodyPart.Health.Maximum = AddHpPerLevel(accLv, charType, bodyPart, baseHealth, increaseHealth.stomach_health_per_level) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.stomach_health_per_level);
                        break;

                    case "LeftArm":
                        bodyPart.Health.Maximum = AddHpPerLevel(accLv, charType, bodyPart, baseHealth, increaseHealth.left_arm_per_level) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.left_arm_per_level);
                        break;

                    case "LeftLeg":
                        bodyPart.Health.Maximum = AddHpPerLevel(accLv, charType, bodyPart, baseHealth, increaseHealth.left_leg_per_level) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.left_leg_per_level);
                        break;

                    case "RightArm":
                        bodyPart.Health.Maximum = AddHpPerLevel(accLv, charType, bodyPart, baseHealth, increaseHealth.right_arm_per_level) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.right_arm_per_level);
                        break;

                    case "RightLeg":
                        bodyPart.Health.Maximum = AddHpPerLevel(accLv, charType, bodyPart, baseHealth, increaseHealth.right_leg_per_level) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.right_leg_per_level);
                        break;

                    default:
                        _logger.Info($"{bodyPartName} is missing");
                        break;
                }
            //_logger.Info(LogPrefix + baseHealth.GetType().ToString());
            CheckIfTooMuchHealth(bodyPartName, bodyPart);
            ResetScavHealthOnLoad(bodyPart, baseHealth);

            //_logger.Success($"{LogPrefix}Modified {bodyPartName} to {bodyPart.Health.Maximum}");
        }

        private double AddHpPerLevel<T, E, G>(double accLv, ICharacter<T, E, G> charType, BodyPartHealth bodyPart, IBaseHealth baseHealth, int increaseHealth)
        {
            return baseHealth.right_leg_base_health + (GetIncrement(accLv, charType) * increaseHealth);
        }

        private double AddHpPerSkillLevel<T, E, G>(ICharacter<T, E, G> charType, double hpSkillv, BodyPartHealth bodyPart, int increasePerHealthSkill)
        {
            return charType.health_per_health_skill_level ?
                Math.Floor(hpSkillv / 100 / charType.health_skill_levels_per_increment) * increasePerHealthSkill :
                0;
        }

        private static double GetHealthLevel<T, E, G>(PmcData character, ICharacter<T, E, G> charType)
        {
            double hpSkillLv = character?.Skills?.Common.Where(a => a.Id == SkillTypes.Health).Select(a => a.Progress)?.FirstOrDefault() ?? 0;
            if (charType.level_health_skill_cap)
            {
                return Math.Min(hpSkillLv, charType.level_health_skill_cap_value);
            }
            else
            {
                return hpSkillLv;
            }
        }

        private int CheckLevelCap<T, E, G>(PmcData character, ICharacter<T, E, G> charType)
        {
            return charType.level_cap ? Math.Min(character.Info.Level.Value, charType.level_cap_value) : character.Info.Level.Value;
        }

        private void ResetScavHealthOnLoad(BodyPartHealth bodyPart, IBaseHealth baseHealth)
        {
            if (baseHealth is Base_Health_SCAV && isOnLoad)
            {
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

        private double GetIncrement<T, E, G>(double accountLevel, ICharacter<T, E, G> charType)
        {
            return Math.Truncate((accountLevel) / (double)charType.levels_per_increment);
        }
    }
}