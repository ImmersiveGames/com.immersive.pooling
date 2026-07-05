# Pooling Boundary

`com.immersive.pooling` is a technical package for generic pooling primitives.

## Owners

- `Immersive.Pooling.Runtime`: pure contracts and policy enums. It has `noEngineReferences: true`.
- `Immersive.Pooling.Unity`: Unity adapters, `GameObject` pooling, authoring assets, optional hosts, and QA helpers.

## Allowed Now

- `IPoolable` and `IPoolLifecycle` callbacks.
- `PoolableBehaviour` as the canonical Unity base.
- `GameObjectPool` as the local pool engine.
- `PoolDefinitionAsset` as explicit authoring identity.
- `IPoolService` and `PoolService` as explicit, non-singleton runtime composition.
- `PoolRuntimeHost` as an optional scene component.
- `PoolReturnHandle` for explicit instance-to-origin return.
- Generic timed auto-return when configured on a pool definition.
- QA context menu helpers that depend on pooling runtime, while runtime does not depend on QA.

## Rejected Or Deferred

- Global singleton service.
- Service locator integration.
- Framework bootstrap or `FrameworkRuntimeHost` dependency.
- Audio implementation.
- VFX, Actor, Projectile, Spawn, Route, Activity, or Session lifecycle ownership.
- Asset discovery by path, `Resources.Load`, or hidden global catalogs.
- String identity for pool lookup.
- Old Base 2.0 namespaces or direct asset/config migration.

## Registration

`PoolRegistrationMode` is intentionally small:

- `LazyOnFirstRent`: `PoolService.Rent` may register the pool.
- `ExplicitPrepareOnly`: callers must call `EnsureRegistered` or `Prewarm` before renting.

Old `ActivityEntry`, `RouteEntry`, and `GlobalBoot` are not package-level concepts. Future framework or Audio modules can map their own lifecycle decisions to explicit calls on `IPoolService`.

## Lifetime

`PoolLifetimeScope` is generic package metadata:

- `Temporary`
- `Persistent`

It does not create framework lifecycle behavior. `PoolService.ClearPoolsForScope` only clears pools already registered under the matching scope.

## Canonical Order

- Create: instantiate inactive, bind handle, notify `IPoolLifecycle.OnCreatedByPool`.
- Rent: mark active, bind handle, set parent, activate, notify `IPoolable.OnTakenFromPool`.
- Return: cancel auto-return, notify `IPoolable.OnReturnedToPool`, deactivate, mark available.
- Clear: cancel timers, notify `IPoolLifecycle.OnDestroyedByPool`, clear handle, destroy instances.

Rejected returns are expected and return `false`.
