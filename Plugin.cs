using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Servers.Http;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace HealthPerLevel_cs;
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.fryciarz7.spt.hpl";
    public override string Name { get; init; } = "Health Per Level";
    public override string Author { get; init; } = "fryciarz7";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("0.1.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");

    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "Creative Commons BY-NC-SA 3.0";
}

[Injectable(TypePriority = 400011)]
public class HealthPerLevelOnLoad : IOnLoad
{
    private const string LogPrefix = "[HealthPerLevel] ";
    private readonly ISptLogger<HealthPerLevelOnLoad> _logger;
    private readonly DatabaseService _databaseService;

    private readonly SaveServer _saveServer;

    private readonly HealthPerLevel _test1;

    public HealthPerLevelOnLoad(
        ISptLogger<HealthPerLevelOnLoad> logger,
        DatabaseService databaseService,
        SaveServer saveServer,
        HealthPerLevel test1)
    {
        this._logger = logger;
        this._databaseService = databaseService;
        this._saveServer = saveServer;

        _test1 = test1;
    }

    public async Task OnLoad()
    {
        await _saveServer.LoadAsync();
        await _test1.DoStuff(true);
        _logger.Info($"{LogPrefix}Mod loaded...");
        await Task.CompletedTask;
    }
}

[Injectable(TypePriority = OnUpdateOrder.InsuranceCallbacks)]
public class HealthPerLevelOnUpdate : IOnUpdate
{
    private const string LogPrefix = "[HealthPerLevel] ";
    private readonly ISptLogger<HealthPerLevelOnLoad> _logger;
    private readonly DatabaseService _databaseService;

    private readonly SaveServer _saveServer;

    private readonly HealthPerLevel _test1;

    public HealthPerLevelOnUpdate(
        ISptLogger<HealthPerLevelOnLoad> logger,
        DatabaseService databaseService,
        SaveServer saveServer,
        HealthPerLevel test1)
    {
        this._logger = logger;
        this._databaseService = databaseService;
        this._saveServer = saveServer;

        _test1 = test1;
    }

    public async Task<bool> OnUpdate(long secondsSinceLastRun)
    {
        await _saveServer.LoadAsync();
        await _test1.DoStuff(false);
        return await Task.FromResult(true);
    }
}