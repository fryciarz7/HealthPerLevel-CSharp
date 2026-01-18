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

        #region Bot Health Modification
        public ValueTask<string> ModifyBotHealth(string? output)
        {
            if (_config.enabled == false || _config.AI.enabled == false || string.IsNullOrWhiteSpace(output))
            {
                return new ValueTask<string>(output ?? "");
            }

            try
            {
                JsonNode? parsed = JsonNode.Parse(output);
                if (parsed is not JsonObject root)
                {
                    _logger.Info($"{LogPrefix}Payload root is not an object, returning original output.");
                    return new ValueTask<string>(output);
                }

                JsonArray? data = root["data"] as JsonArray;
                if (data == null)
                {
                    _logger.Info($"{LogPrefix}No data array found, returning original output.");
                    return new ValueTask<string>(output);
                }

                foreach (JsonNode? botNode in data)
                {
                    if (botNode is not JsonObject bot)
                        continue;

                    string botRole = GetBotRole(bot);
                    if (string.IsNullOrEmpty(botRole))
                        continue;

                    if (ShouldSkipBotRole(botRole))
                        continue;

                    int botLevel = bot["Info"]?["Level"]?.GetValue<int>() ?? 1;

                    // Use PMC config for bot calculations like original code did.
                    var charType = _config.PMC;
                    double increment = GetIncrement(botLevel, charType);

                    var bodyParts = bot["Health"]?["BodyParts"] as JsonObject;
                    if (bodyParts == null)
                        continue;

                    foreach (var part in bodyParts)
                    {
                        if (part.Value is not JsonObject partObj)
                            continue;

                        var healthNode = partObj["Health"] as JsonObject;
                        if (healthNode == null)
                            continue;

                        double newMax = CalculateBotNewMaxHealth(part.Key, charType, increment);
                        // Ensure values stored as JsonValue
                        healthNode["Maximum"] = JsonValue.Create(newMax);
                        healthNode["Current"] = JsonValue.Create(newMax);
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
                _logger.Error($"{LogPrefix}ModifyBotHealth failed: {ex}");
                if (_config.debug)
                {
                    _logger.Error($"{LogPrefix}inner message: {ex?.InnerException?.Message ?? ""}");
                    _logger.Error($"{LogPrefix}StackTrace: {ex?.StackTrace}");
                }
                return new ValueTask<string>(output ?? "");
            }
        }

        private static string GetBotRole(JsonObject bot)
        {
            try
            {
                return bot["Info"]?["Settings"]?["Role"]?.GetValue<string>() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool ShouldSkipBotRole(string role)
        {
            // Mirror original behavior, but safer (null-guarded)
            if (string.IsNullOrWhiteSpace(role))
                return true;

            switch (role)
            {
                case "pmcUSEC":
                case "pmcBEAR":
                    return !_config.AI.pmc_bot_health;

                case "assault":
                case "marksman":
                case "cursedassault":
                    return !_config.AI.scav_bot_health;

                case "gifter":
                case "exUsec":
                case "shooterBTR":
                    return !_config.AI.special_bot_health;

                case "pmcBot": // Raider bots
                    return !_config.AI.raider_bot_health;

                case "sectactPriest":
                case "sectactPriestEvent":
                    return !_config.AI.cultist_bot_health;

                case "infectedAssault":
                case "infectedPmc":
                case "arenaFighterEvent":
                    return !_config.AI.event_boss_health;

                default:
                    // handle prefixes for boss/follower
                    if (role.StartsWith("boss") || role.StartsWith("follower"))
                        return !_config.AI.boss_bot_health;
                    break;
            }

            return false;
        }

        private double CalculateBotNewMaxHealth<T, E, G>(string partKey, ICharacter<T, E, G> charType, double increment)
        {
            // Cast to IHealth for convenience
            IHealth baseHealth = charType.base_health as IHealth;
            IHealth increaseHealth = charType.increase_per_level as IHealth;
            if (baseHealth == null || increaseHealth == null)
                return 0;

            return partKey switch
            {
                "Head" => AddHpPerLevel(increment, charType, null, baseHealth.head_health, increaseHealth.head_health),
                "Chest" => AddHpPerLevel(increment, charType, null, baseHealth.thorax_health, increaseHealth.thorax_health),
                "Stomach" => AddHpPerLevel(increment, charType, null, baseHealth.stomach_health, increaseHealth.stomach_health),
                "LeftArm" => AddHpPerLevel(increment, charType, null, baseHealth.left_arm_health, increaseHealth.left_arm_health),
                "LeftLeg" => AddHpPerLevel(increment, charType, null, baseHealth.left_leg_health, increaseHealth.left_leg_health),
                "RightArm" => AddHpPerLevel(increment, charType, null, baseHealth.right_arm_health, increaseHealth.right_arm_health),
                "RightLeg" => AddHpPerLevel(increment, charType, null, baseHealth.right_leg_health, increaseHealth.right_leg_health),
                _ => 0,
            };
        }

        #endregion Bot Health Modification

        private void HpChanges(bool restoreDefault = false)
        {
            var profiles = _saveServer.GetProfiles();

            foreach (var kvp in profiles)
            {
                try
                {
                    SptProfile? profile = kvp.Value;
                    //_logger.Info($"{LogPrefix}Modifying health for profile: {profile?.ProfileInfo?.Username} with experience: {profile?.CharacterData?.PmcData?.Info?.Experience}");
                    _logger.Info($"{LogPrefix}Modifying health for profile: {profile?.ProfileInfo?.Username}");
                    if (profile?.CharacterData?.PmcData != null)
                    {
                        CalculateCharacterData(profile.CharacterData.PmcData, _config.PMC);
                    }
                    if (profile?.CharacterData?.ScavData != null)
                    {
                        CalculateCharacterData(profile.CharacterData.ScavData, _config.SCAV);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"{LogPrefix}Error: {ex.Message}");
                    if (_config.debug)
                    {
                        _logger.Error($"{LogPrefix}inner message: {ex?.InnerException?.Message ?? ""}");
                        _logger.Error($"{LogPrefix}StackTrace: {ex?.StackTrace}");
                    }
                }
            }
        }

        private void CalculateCharacterData<T, E, G>(PmcData character, ICharacter<T, E, G> charType, bool restoreDefault)
        {
            ValidateProfile(character, charType);
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

        private void ValidateProfile<T, E, G>(PmcData character, ICharacter<T, E, G> charType)
        {
            if (character.Info == null)
            {
                throw new Exception($"Character info is null. Expected if new profile.");
            }
            if (character.Health == null || character.Health.BodyParts == null)
            {
                throw new Exception($"Character health or body parts data is null.");
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
            try
            {
                double hpSkillLv = character?.Skills?.Common.FirstOrDefault(a => a.Id == SkillTypes.Health)?.Progress ?? 0;
                return charType.level_health_skill_cap ? Math.Min(hpSkillLv, charType.level_health_skill_cap_value) : hpSkillLv;
            }
            catch (Exception)
            {
                throw new Exception($"Health skill level missing.");
            }
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