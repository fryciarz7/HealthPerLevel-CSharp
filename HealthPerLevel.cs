using HealthPerLevel_cs.config;
using HealthPerLevel_cs.Interfaces;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        public ValueTask<string> ModifyBotHealth(string? output)
        {
            if (_config.AI.enabled == false || output == null)
            {
                return new ValueTask<string>(output ?? "");
            }
            try
            {
                var root = JsonNode.Parse(output)!.AsObject();
                var data = root["data"]!.AsArray();

                foreach (var bot in data)
                {
                    var botRole = bot?["Info"]?["Settings"]?["Role"]?.GetValue<string>();
                    switch (botRole)
                    {
                        case "pmcUSEC":
                        case "pmcBEAR":
                            if (!_config.AI.pmc_bot_health)
                            {
                                continue;
                            }
                            break;
                        case "assault":
                        case "marksman":
                        case "cursedassault":
                            if (!_config.AI.scav_bot_health)
                            {
                                continue;
                            }
                            break;
                            break;
                        case "gifter":
                        case "exUsec":
                        case "shooterBTR":
                            if (!_config.AI.special_bot_health)
                            {
                                continue;
                            }
                            break;
                        case "pmcBot": // Raider bots
                            if (!_config.AI.raider_bot_health)
                            {
                                continue;
                            }
                            break;
                        case "sectactPriest":
                        case "sectactPriestEvent":
                            if (!_config.AI.cultist_bot_health)
                            {
                                continue;
                            }
                            break;
                        case "infectedAssault":
                        case "infectedPmc":
                        case "arenaFighterEvent":
                            if (!_config.AI.event_boss_health)
                            {
                                continue;
                            }
                            break;
                        default:
                            if (botRole.StartsWith("boss") && !_config.AI.boss_bot_health)
                            {
                                continue;
                            }
                            if (botRole.StartsWith("follower") && !_config.AI.boss_bot_health)
                            {
                                continue;
                            }
                            break;
                    }

                    var botLevel = bot?["Info"]?["Level"]?.GetValue<int>() ?? 1;
                    ICharacter<Base_Health_PMC, Increase_Per_Level_PMC, Increase_Per_Health_Skill_Level_PMC> charType = _config.PMC;
                    double increment = GetIncrement(botLevel, charType);
                    IHealth base_health = charType.base_health;
                    IHealth increaseHealth = charType.increase_per_level;

                    var bodyParts =
                        bot?["Health"]?["BodyParts"] as JsonObject;

                    if (bodyParts == null)
                        continue;

                    foreach (var part in bodyParts)
                    {

                        var health = part.Value?["Health"] as JsonObject;
                        if (health == null)
                            continue;

                        double newMaxHealth = 0;
                        switch (part.Key)
                        {
                            case "Head":
                                newMaxHealth = AddHpPerLevel(increment, charType, null, base_health.head_health, increaseHealth.head_health);
                                break;

                            case "Chest":
                                newMaxHealth = AddHpPerLevel(increment, charType, null, base_health.thorax_health, increaseHealth.thorax_health);
                                break;

                            case "Stomach":
                                newMaxHealth = AddHpPerLevel(increment, charType, null, base_health.stomach_health, increaseHealth.stomach_health);
                                break;

                            case "LeftArm":
                                newMaxHealth = AddHpPerLevel(increment, charType, null, base_health.left_arm_health, increaseHealth.left_arm_health);
                                break;

                            case "LeftLeg":
                                newMaxHealth = AddHpPerLevel(increment, charType, null, base_health.left_leg_health, increaseHealth.left_leg_health);
                                break;

                            case "RightArm":
                                newMaxHealth = AddHpPerLevel(increment, charType, null, base_health.right_arm_health, increaseHealth.right_arm_health);
                                break;

                            case "RightLeg":
                                newMaxHealth = AddHpPerLevel(increment, charType, null, base_health.right_leg_health, increaseHealth.right_leg_health);
                                break;
                            default:
                                break;
                        }

                        health["Maximum"] = newMaxHealth;
                        health["Current"] = newMaxHealth;
                    }
                }

                string outputJson = root.ToJsonString(
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                );
                return new ValueTask<string>(outputJson);
            }
            catch (Exception ex)
            {

                _logger.Error($"{ex}");
            }

            return new ValueTask<string>(output);
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
            IHealth baseHealth = charType.base_health as IHealth;
            IHealth increaseHealth = charType.increase_per_level as IHealth;
            IHealth increasePerHealthSkill = charType.increase_per_health_skill_level as IHealth;

            double increment = GetIncrement(accLv, charType);

            if (charType.health_per_health_skill_level)

                switch (bodyPartName)
                {
                    case "Head":
                        bodyPart.Health.Maximum = AddHpPerLevel(increment, charType, bodyPart, baseHealth.head_health, increaseHealth.head_health) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.head_health);
                        break;

                    case "Chest":
                        bodyPart.Health.Maximum = AddHpPerLevel(increment, charType, bodyPart, baseHealth.thorax_health, increaseHealth.thorax_health) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.thorax_health);
                        break;

                    case "Stomach":
                        bodyPart.Health.Maximum = AddHpPerLevel(increment, charType, bodyPart, baseHealth.stomach_health, increaseHealth.stomach_health) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.stomach_health);
                        break;

                    case "LeftArm":
                        bodyPart.Health.Maximum = AddHpPerLevel(increment, charType, bodyPart, baseHealth.left_arm_health, increaseHealth.left_arm_health) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.left_arm_health);
                        break;

                    case "LeftLeg":
                        bodyPart.Health.Maximum = AddHpPerLevel(increment, charType, bodyPart, baseHealth.left_leg_health, increaseHealth.left_leg_health) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.left_leg_health);
                        break;

                    case "RightArm":
                        bodyPart.Health.Maximum = AddHpPerLevel(increment, charType, bodyPart, baseHealth.right_arm_health, increaseHealth.right_arm_health) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.right_arm_health);
                        break;

                    case "RightLeg":
                        bodyPart.Health.Maximum = AddHpPerLevel(increment, charType, bodyPart, baseHealth.right_leg_health, increaseHealth.right_leg_health) +
                            AddHpPerSkillLevel(charType, hpSkillv, bodyPart, increasePerHealthSkill.right_leg_health);
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

        private double AddHpPerLevel<T, E, G>(double inrement, ICharacter<T, E, G> charType, BodyPartHealth bodyPart, int baseHealth, int increaseHealth)
        {
            return baseHealth + (inrement * increaseHealth);
        }

        private double AddHpPerSkillLevel<T, E, G>(ICharacter<T, E, G> charType, double hpSkillv, BodyPartHealth bodyPart, int increasePerHealthSkill)
        {
            return charType.health_per_health_skill_level ?
                Math.Floor(hpSkillv / 100 / charType.health_skill_levels_per_increment) * increasePerHealthSkill :
                0;
        }

        private static double GetHealthLevel<T, E, G>(PmcData character, ICharacter<T, E, G> charType)
        {
            double hpSkillLv = character?.Skills?.Common.FirstOrDefault(a => a.Id == SkillTypes.Health)?.Progress ?? 0;
            return charType.level_health_skill_cap ? Math.Min(hpSkillLv, charType.level_health_skill_cap_value) : hpSkillLv;
        }

        private int CheckLevelCap<T, E, G>(PmcData character, ICharacter<T, E, G> charType)
        {
            return charType.level_cap ? Math.Min(character.Info.Level.Value, charType.level_cap_value) : character.Info.Level.Value;
        }

        private void ResetScavHealthOnLoad(BodyPartHealth bodyPart, IHealth baseHealth)
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