using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HealthPerLevel_cs;
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.fryciarz7.spt.hpl";
    public override string Name { get; init; } = "Health Per Level";
    public override string Author { get; init; } = "fryciarz7";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.0");
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

    private readonly HealthPerLevel _hpl;

    public HealthPerLevelOnLoad(
        ISptLogger<HealthPerLevelOnLoad> logger,
        DatabaseService databaseService,
        SaveServer saveServer,
        HealthPerLevel hpl)
    {
        this._logger = logger;
        this._databaseService = databaseService;
        this._saveServer = saveServer;

        _hpl = hpl;
    }

    public async Task OnLoad()
    {
        await _saveServer.LoadAsync();
        await _hpl.DoStuff(true);
        _logger.Info($"{LogPrefix}Mod loaded...");
        await Task.CompletedTask;
    }
}

[Injectable(TypePriority = OnUpdateOrder.InsuranceCallbacks)]
public class HealthPerLevelOnUpdate : IOnUpdate
{
    private readonly SaveServer _saveServer;

    private readonly HealthPerLevel _hpl;

    public HealthPerLevelOnUpdate(
        SaveServer saveServer,
        HealthPerLevel hpl)
    {
        this._saveServer = saveServer;

        _hpl = hpl;
    }

    public async Task<bool> OnUpdate(long secondsSinceLastRun)
    {
        await _saveServer.LoadAsync();
        await _hpl.DoStuff(false);
        return await Task.FromResult(true);
    }
}

[Injectable]
public class BotHealthGenerateRoute(JsonUtil jsonUtil, BotHealthGenerateCallbacks callbacks) : StaticRouter(
    jsonUtil, [
        new RouteAction<GenerateBotsRequestData>(
                "/client/game/bot/generate",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) => await callbacks.HandleGenerateBotsRoute(url, info, sessionId, output)
                )
        ]
    )
{ }



/// <summary>
/// This class handles callbacks that are sent to your route, you can run code both synchronously here as well as asynchronously
/// </summary>
[Injectable]
public class BotHealthGenerateCallbacks(ISptLogger<BotHealthGenerateCallbacks> logger, ModHelper modHelper, HttpResponseUtil httpResponseUtil,
        HealthPerLevel hpl)
{
    public ValueTask<string> HandleGenerateBotsRoute(string url, GenerateBotsRequestData info, MongoId sessionId, string? output)
    {
        return hpl.ModifyBotHealth(output);
    }
}