# Pooling Manual QA Smoke

Use the [Pooling Usage Guide](Pooling-Usage-Guide.md) for canonical asset authoring, `PoolService`/`PoolRuntimeHost` composition, return behavior, lifetime, cleanup, and errors. This page keeps only the package-local manual QA entry procedure and its Context Menu actions.

## Manual QA smoke

This is a manually invoked smoke helper, not an automated test or a certification run. In a Unity scene:

1. Create and configure a `PoolDefinitionAsset` using the [canonical authoring procedure](Pooling-Usage-Guide.md#3-criar-um-pooldefinitionasset).
2. Add a `PoolRuntimeHost` to a scene GameObject and assign the definition.
3. Add `PoolingQaContextMenuDriver` to a QA GameObject; assign the host and definition. Optionally assign `Rent Parent` and set `Burst Count`.
4. Invoke actions in this order from the component's Context Menu:

   - `Pooling QA/Ensure Pool`
   - `Pooling QA/Prewarm`
   - `Pooling QA/Rent One`
   - `Pooling QA/Rent Burst`
   - `Pooling QA/Return Last`
   - `Pooling QA/Return All`
   - `Pooling QA/Clear Pool`

5. Confirm rented instances are created under the configured parent, returned instances become available again, and clearing removes the pool's instances.

`Run Basic Scenario` performs Ensure Pool, Prewarm, Rent One, Return Last, Rent Burst, and Return All. It does not call Clear Pool. The driver requires an assigned `PoolDefinitionAsset` and a `PoolRuntimeHost`; it attempts to find the host in its parents and initializes it if needed. Missing references throw `MissingReferenceException` rather than silently skipping the action.

The host also exposes these separate Context Menu operations: `Pooling/Initialize`, `Pooling/Prewarm All`, `Pooling/Return All`, `Pooling/Clear All`, and `Pooling/Shutdown`. These act on the host's configured definitions and are not part of the QA driver's `Run Basic Scenario`.

The helper retains its rented-object list for the sequence. Use `Return All` before `Clear Pool`; after clearing, the driver clears its list. This QA driver lives in the package's Unity runtime assembly and exposes editor Context Menu actions. No automated package tests or shipped `Samples~` are provided.
