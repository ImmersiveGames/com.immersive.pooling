# Pooling How-To

## PoolDefinitionAsset

Create a `PoolDefinitionAsset` from `Immersive > Pooling > Pool Definition`.

Required fields:

- `Prefab`: prefab reused by the pool.
- `Initial Capacity`: number of instances to create when prewarming.
- `Max Size`: hard cap for created instances.
- `Can Expand`: allows rent to create instances beyond the initial capacity.

Behavior fields:

- `Prewarm On Register`: `EnsureRegistered` creates `Initial Capacity`.
- `Registration Mode`: `LazyOnFirstRent` can register during rent; `ExplicitPrepareOnly` requires an earlier `EnsureRegistered` or `Prewarm`.
- `Auto Return Seconds`: optional generic timed return. `0` disables it.

## Explicit Service

```csharp
var service = new PoolService();
service.EnsureRegistered(definition);

var instance = service.Rent(definition, parent);

service.Return(definition, instance);
service.Shutdown();
```

Use the `PoolDefinitionAsset` reference as identity. Do not use labels or strings to resolve a pool.

## Scene Host

Add `PoolRuntimeHost` to a scene object and assign definitions. The host creates a local `PoolService`; it does not register globally.

Useful context menu actions:

- `Pooling/Initialize`
- `Pooling/Prewarm All`
- `Pooling/Return All`
- `Pooling/Clear All`
- `Pooling/Shutdown`

## Instance Return

Rented objects receive a `PoolReturnHandle` automatically.

```csharp
var handle = GetComponent<PoolReturnHandle>();
handle.ReturnToPool();
```

Duplicate or foreign returns are rejected with `false`.

## QA

Use `PoolingQaContextMenuDriver` with a `PoolRuntimeHost` and one `PoolDefinitionAsset`.

Suggested flow:

1. `Pooling QA/Ensure Pool`
2. `Pooling QA/Prewarm`
3. `Pooling QA/Rent One`
4. `Pooling QA/Rent Burst`
5. `Pooling QA/Return Last`
6. `Pooling QA/Return All`
7. `Pooling QA/Clear Pool`
