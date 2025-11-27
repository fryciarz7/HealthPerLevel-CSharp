using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;

namespace HealthPerLevel_cs;
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.fryciarz7.spt.hpl";
    public override string Name { get; init; } = "Health Per Level";
    public override string Author { get; init; } = "fryciarz7";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("0c.1.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");

    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string? License { get; init; } = "Creative Commons BY-NC-SA 3.0";
}

[Injectable]
public class HealthPerLevel
{
}