# Immersive Pooling

Generic Unity pooling primitives for the Immersive Framework package set.

## Installation

Configure OpenUPM for the `com.immersive` scope and add
`com.immersive.pooling` version `0.2.2` to `Packages/manifest.json`.

Git fallback: `https://github.com/ImmersiveGames/com.immersive.pooling.git#v0.2.2`.

## Role

`com.immersive.pooling` owns reusable pooling mechanics only:

- pure contracts in `Immersive.Pooling.Runtime`;
- Unity `GameObject` pooling in `Immersive.Pooling.Unity`;
- authoring data through `PoolDefinitionAsset`;
- explicit composition through `PoolService` or optional `PoolRuntimeHost`.

It does not own framework bootstrap, Audio, VFX, gameplay identity, scene flow, service location, or singleton registration.

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

## Using PoolRuntimeHost

Add `PoolRuntimeHost` to a scene object, assign the pool definitions, and let it initialize on `Awake` or call `Initialize` manually. The host is optional composition; it is not a global singleton and does not depend on `com.immersive.framework`.

## Returning From An Instance

Instances rented from `GameObjectPool` receive a `PoolReturnHandle`. Code on the instance can call:

```csharp
GetComponent<PoolReturnHandle>().ReturnToPool();
```

Invalid or duplicate returns return `false` and do not corrupt pool state.

## Future Audio Use

Audio can later hold explicit `PoolDefinitionAsset` references and receive an `IPoolService` from its own composition path. This package does not implement Audio behavior, Audio fallback policy, voice budgeting, or Audio bootstrap.

## Manual Smoke

1. Create a `PoolDefinitionAsset` with a simple prefab.
2. Add `PoolRuntimeHost` and assign the definition.
3. Prewarm.
4. Rent or spawn three objects.
5. Return one object.
6. Return all.
7. Clear the pool.
8. Confirm `PoolableBehaviour` callbacks and active/inactive counts.

## License

Licensed under the [MIT License](LICENSE.md).
