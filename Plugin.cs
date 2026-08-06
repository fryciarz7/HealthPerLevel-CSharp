using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HealthPerLevel_cs;
public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.fryciarz7.spt.hpl";
    public string Name { get; init; } = "Health Per Level";
    public string Author { get; init; } = "fryciarz7";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("2.2.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");

    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public bool? IsBundleMod { get; init; }
    public string? License { get; init; } = "Creative Commons BY-NC-SA 3.0";
    public bool HasPrepatcher { get; init; }
}

[Injectable]
public class HealthChangesRoute(JsonUtil jsonUtil, HealthChangesCallbacks callbacks) : StaticRouter(
    jsonUtil, [
        new RouteAction<GenerateBotsRequestData>(
            "/client/game/bot/generate",
            async (
                url,
                info,
                sessionId,
                output,
                cancellationToken
                ) => await callbacks.HandleGenerateBotsRoute(url, info, sessionId, output, cancellationToken)
            ),
        new RouteAction<EmptyRequestData>(
            // "/client/game/start",
            "/client/game/profile/select",
            async(
                url,
                info,
                sessionId,
                output,
                cancellationToken
                ) => await callbacks.HandleProfileSelectRoute(url, info, sessionId, output, cancellationToken)
            ),
        new RouteAction<EmptyRequestData>(
            "/client/game/start",
            async(
                url,
                info,
                sessionId,
                output,
                cancellationToken
                ) => await callbacks.HandleGameStartRoute(url, info, sessionId, output, cancellationToken)
            )
        ]
    )
{ }



/// <summary>
/// This class handles callbacks that are sent to your route, you can run code both synchronously here as well as asynchronously
/// </summary>
[Injectable]
public class HealthChangesCallbacks(ISptLogger<HealthChangesCallbacks> logger, ModHelper modHelper, HttpResponseUtil httpResponseUtil,
        HealthPerLevel hpl)
{
    public ValueTask<string> HandleGenerateBotsRoute(string url, GenerateBotsRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken)
    {
        return hpl.ModifyBotHealth(output);
    }
    public ValueTask<string> HandleProfileSelectRoute(string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken)
    {
        hpl.DoStuff(false);
        return ValueTask.FromResult(output);
    }

    internal ValueTask<string> HandleGameStartRoute(string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken)
    {
        hpl.DoStuff(true);
        logger.Info("[HealthPerLevel] Game started, health adjusted.");
        return ValueTask.FromResult(output);
    }
}