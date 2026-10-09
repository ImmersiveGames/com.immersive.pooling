# Immersive Pooling

Generic Unity pooling primitives for the Immersive Framework package set.

## Installation

Use the package source already configured by the consuming project. Resolve the installed version from `Packages/manifest.json` and `packages-lock.json`; this README does not pin a version for discovery.

## Role

`com.immersive.pooling` owns reusable pooling mechanics only:

- pure contracts in `Immersive.Pooling.Runtime`;
- Unity `GameObject` pooling in `Immersive.Pooling.Unity`;
- authoring data through `PoolDefinitionAsset`;
- explicit composition through `PoolService` or optional `PoolRuntimeHost`.

It does not own framework bootstrap, Audio, VFX, gameplay identity, scene flow, service location, or singleton registration.

## Find a capability

The [Pooling Usage Guide](Documentation~/Pooling-Usage-Guide.md) is the canonical authoring and runtime procedure. The [Boundary](Documentation~/Pooling-Boundary.md) defines ownership and package limits. [Pooling How-To](Documentation~/Pooling-How-To.md) is retained for the package's manual QA context-menu procedure.

| Intent | Guide |
|---|---|
| Author a pool asset and choose registration behavior | [Asset authoring](Documentation~/Pooling-Usage-Guide.md#3-criar-um-pooldefinitionasset), [Registration Mode](Documentation~/Pooling-Usage-Guide.md#6-registration-mode) |
| Use a scene-local host | [PoolRuntimeHost](Documentation~/Pooling-Usage-Guide.md#4-uso-por-cena-com-poolruntimehost) |
| Own a service explicitly | [PoolService](Documentation~/Pooling-Usage-Guide.md#5-uso-explícito-com-poolservice) |
| Rent, return, auto-return, inspect or clean up instances | [Return](Documentation~/Pooling-Usage-Guide.md#9-devolver-uma-instância-ao-pool), [Auto-return](Documentation~/Pooling-Usage-Guide.md#10-auto-return), [Snapshots](Documentation~/Pooling-Usage-Guide.md#11-snapshots-e-contadores), [Cleanup](Documentation~/Pooling-Usage-Guide.md#12-limpeza) |
| Run the available manual smoke | [Manual QA procedure](Documentation~/Pooling-How-To.md#manual-qa-smoke) |

## Core API

- `IPoolable`: minimal take/return callbacks.
- `IPoolLifecycle`: optional created/destroyed callbacks.
- `PoolableBehaviour`: `MonoBehaviour` base implementing pool state and lifecycle callbacks.
- `PoolDefinitionAsset`: prefab, capacity, expansion, registration, lifetime, prewarm, and auto-return settings.
- `GameObjectPool`: local instantiable pool with `Rent`, `Spawn`, `Return`, `ReturnAll`, `Clear`, active/inactive counts, max size, and expansion policy.
- `IPoolService`: explicit service contract over `PoolDefinitionAsset`.
- `PoolService`: non-singleton service that owns registered pools by asset reference.
- `PoolRuntimeHost`: optional scene component that composes a `PoolService` and declared pool definitions.
- `PoolReturnHandle`: component added to pooled instances so an object can return itself to its origin pool.
- `PoolRuntimeSnapshot`: active, inactive, and total counts exposed by `IPoolService.TryGetSnapshot`.

## Creating A PoolDefinitionAsset

1. Create `Assets > Create > Immersive > Pooling > Pool Definition`.
2. Assign a prefab.
3. Set `Initial Capacity`, `Max Size`, and `Can Expand`.
4. Enable `Prewarm On Register` when `EnsureRegistered` should create the initial instances.
5. Choose `LazyOnFirstRent` if rent may auto-register the pool, or `ExplicitPrepareOnly` if a host/consumer must register it first.
6. Set `Auto Return Seconds` only for generic timed return behavior. Use `0` to disable.

## Using PoolService Explicitly

```csharp
var service = new PoolService();
service.EnsureRegistered(definition);
var instance = service.Rent(definition, parent);
service.Return(definition, instance);
service.ReturnAll(definition);
service.Clear(definition);
service.Shutdown();
```

`PoolDefinitionAsset` is the identity. The package does not parse strings to resolve pools.

The service instance owns its registered pools. Call `Shutdown` when the composition owner ends; after shutdown, service operations throw. `PoolLifetimeScope` only labels definitions for explicit `ClearPoolsForScope` calls. It does not automatically follow Route, Activity, scene, or Framework lifecycle.

## Using PoolRuntimeHost

Add `PoolRuntimeHost` to a scene object, assign the pool definitions, and let it initialize on `Awake` or call `Initialize` manually. The host is optional composition; it is not a global singleton and does not depend on `com.immersive.framework`.

## Returning From An Instance

Instances rented from `GameObjectPool` receive a `PoolReturnHandle`. Code on the instance can call:

```csharp
GetComponent<PoolReturnHandle>().ReturnToPool();
```

Invalid or duplicate returns return `false` and do not corrupt pool state.

## Audio integration boundary

`com.immersive.audio` uses explicit `PoolDefinitionAsset` references and `IPoolService` composition for pooled SFX. Pooling does not implement Audio behavior, Audio fallback policy, voice budgeting, or Audio bootstrap.

## Validation

The package provides a manually invoked QA context-menu driver, not an automated test suite. Follow the [manual QA procedure](Documentation~/Pooling-How-To.md#manual-qa-smoke). The procedure checks authoring and pool operations but is not evidence that automated tests ran.

## License

Licensed under the [MIT License](LICENSE.md).
