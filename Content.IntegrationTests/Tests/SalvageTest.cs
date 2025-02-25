﻿using System.Linq;
using Content.Server.Salvage;
using Content.Shared.CCVar;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class SalvageTest
{
    /// <summary>
    /// Asserts that all salvage maps have been saved as grids and are loadable.
    /// </summary>
    [Test]
    public async Task AllSalvageMapsLoadableTest()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapLoader = entManager.System<MapLoaderSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var cfg = server.ResolveDependency<IConfigurationManager>();
        Assert.That(cfg.GetCVar(CCVars.GridFill), Is.False);

        await server.WaitPost(() =>
        {
            foreach (var salvage in prototypeManager.EnumeratePrototypes<SalvageMapPrototype>())
            {
                var mapFile = salvage.MapPath;

                var mapId = mapManager.CreateMap();
                try
                {
                    Assert.That(mapLoader.TryLoad(mapId, mapFile.ToString(), out var roots));
                    Assert.That(roots.Where(uid => entManager.HasComponent<MapGridComponent>(uid)), Is.Not.Empty);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load salvage map {salvage.ID}, was it saved as a map instead of a grid?", ex);
                }

                try
                {
                    mapManager.DeleteMap(mapId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete salvage map {salvage.ID}", ex);
                }
            }
        });
        await server.WaitRunTicks(1);

        await pairTracker.CleanReturnAsync();
    }
}
