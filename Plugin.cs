using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
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
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.fryciarz7.spt.hpl";
    public override string Name { get; init; } = "Health Per Level";
    public override string Author { get; init; } = "fryciarz7";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.1");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.11");

    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "Creative Commons BY-NC-SA 3.0";
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
                    output
                ) => await callbacks.HandleGenerateBotsRoute(url, info, sessionId, output)
                ),
        new RouteAction<EmptyRequestData>(
            // "/client/game/start",
            "/client/game/profile/select",
            async(
                url,
                info,
                sessionId,
                output
                ) => await callbacks.HandleProfileSelectRoute(url, info, sessionId, output)
            ),
        new RouteAction<EmptyRequestData>(
            "/client/game/start",
            async(
                url,
                info,
                sessionId,
                output
                ) => await callbacks.HandleGameStartRoute(url, info, sessionId, output)
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
    public ValueTask<string> HandleGenerateBotsRoute(string url, GenerateBotsRequestData info, MongoId sessionId, string? output)
    {
        return hpl.ModifyBotHealth(output);
    }
    public ValueTask<string> HandleProfileSelectRoute(string url, EmptyRequestData info, MongoId sessionId, string? output)
    {
        hpl.DoStuff(false);
        return ValueTask.FromResult(output);
    }

    internal ValueTask<string> HandleGameStartRoute(string url, EmptyRequestData info, MongoId sessionId, string? output)
    {
        hpl.DoStuff(true);
        logger.Info("[HealthPerLevel] Game started, health adjusted.");
        return ValueTask.FromResult(output);
    }
}