# Immersive Pooling — Guia de Uso

Este guia explica como usar o package `com.immersive.pooling` depois do corte `POST-RESET-E`. O package fornece pooling genérico para `GameObject`, sem depender do `com.immersive.framework`, Audio, cenas de jogo ou singleton global.

## 1. Papel do package

`com.immersive.pooling` é um motor técnico de pooling. Ele resolve:

- criar e reutilizar instâncias de prefabs;
- controlar capacidade inicial, limite máximo e expansão;
- devolver objetos individualmente ou em lote;
- limpar pools;
- emitir callbacks de ciclo de vida para componentes pooláveis;
- expor snapshots simples de `active`, `inactive` e `total`;
- permitir auto-return temporizado quando configurado.

Ele não resolve:

- bootstrap do framework;
- descoberta global automática;
- Audio;
- VFX;
- lifecycle de Route/Activity;
- gameplay identity;
- service locator ou singleton.

## 2. Conceitos principais

| Conceito | Papel |
|---|---|
| `PoolDefinitionAsset` | Asset de authoring que define prefab, capacidade, expansão, lifetime, registration mode e auto-return. |
| `GameObjectPool` | Pool local instanciável para um prefab. Útil para uso direto em código. |
| `PoolService` | Serviço explícito que registra pools por `PoolDefinitionAsset`. Não é singleton. |
| `PoolRuntimeHost` | Componente opcional de cena que cria um `PoolService` local e registra definitions declaradas. |
| `PoolableBehaviour` | Base Unity para componentes que querem reagir a create/rent/return/destroy. |
| `PoolReturnHandle` | Componente adicionado à instância para ela poder se devolver ao pool de origem. |
| `PoolRuntimeSnapshot` | Snapshot com `ActiveCount`, `InactiveCount` e `TotalCount`. |

## 3. Criar um `PoolDefinitionAsset`

No Unity:

```text
Create > Immersive > Pooling > Pool Definition
```

Configure:

| Campo | Uso |
|---|---|
| `Prefab` | Prefab que será instanciado/reutilizado. Obrigatório. |
| `Pool Label` | Nome humano do pool. Se vazio, usa o nome do asset. |
| `Initial Capacity` | Quantidade usada por `Prewarm`. |
| `Max Size` | Limite máximo de instâncias criadas. |
| `Can Expand` | Permite criar além da capacidade inicial até `Max Size`. |
| `Prewarm On Register` | `EnsureRegistered` já cria `Initial Capacity`. |
| `Lifetime Scope` | Metadado genérico: `Temporary` ou `Persistent`. Não acopla ao framework. |
| `Registration Mode` | `LazyOnFirstRent` ou `ExplicitPrepareOnly`. |
| `Auto Return Seconds` | Retorno automático após X segundos. `0` desativa. |

Regras de validação atuais:

- `Prefab` é obrigatório;
- `Initial Capacity >= 0`;
- `Max Size > 0`;
- `Initial Capacity <= Max Size`;
- se `Can Expand = false`, `Initial Capacity` precisa ser maior que `0`;
- `Auto Return Seconds >= 0`.

## 4. Uso por cena com `PoolRuntimeHost`

Use esse modo quando quiser compor pools diretamente numa cena de QA, demo, gameplay ou sistema específico.

### Passos

1. Crie um GameObject, por exemplo `PoolingRuntime`.
2. Adicione `PoolRuntimeHost`.
3. Adicione uma ou mais `PoolDefinitionAsset` na lista `Pool Definitions`.
4. Deixe `Initialize On Awake` habilitado ou chame `Initialize()` manualmente.
5. Chame `PrewarmAll()` se quiser preparar todos os pools declarados.

O host oferece context menus:

```text
Pooling/Initialize
Pooling/Prewarm All
Pooling/Return All
Pooling/Clear All
Pooling/Shutdown
```

`PoolRuntimeHost` não registra nada globalmente. Ele apenas cria um `PoolService` local, acessível por `host.Service`.

### Exemplo

```csharp
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Hosts;
using UnityEngine;

public sealed class ExamplePoolUser : MonoBehaviour
{
    [SerializeField] private PoolRuntimeHost poolHost;
    [SerializeField] private PoolDefinitionAsset projectilePool;
    [SerializeField] private Transform spawnParent;

    public void SpawnProjectile()
    {
        var instance = poolHost.Service.Rent(projectilePool, spawnParent);
        instance.transform.position = transform.position;
    }

    public void ReturnEverything()
    {
        poolHost.Service.ReturnAll(projectilePool);
    }
}
```

## 5. Uso explícito com `PoolService`

Use esse modo quando um sistema próprio quiser possuir o serviço diretamente, sem `PoolRuntimeHost`.

```csharp
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Runtime;
using UnityEngine;

public sealed class ExplicitPoolExample : MonoBehaviour
{
    [SerializeField] private PoolDefinitionAsset definition;
    [SerializeField] private Transform poolRoot;
    [SerializeField] private Transform spawnParent;

    private PoolService service;

    private void Awake()
    {
        service = new PoolService(poolRoot);
        service.EnsureRegistered(definition);
        service.Prewarm(definition);
    }

    public GameObject Rent()
    {
        return service.Rent(definition, spawnParent);
    }

    public void Return(GameObject instance)
    {
        service.Return(definition, instance);
    }

    private void OnDestroy()
    {
        service?.Shutdown();
    }
}
```

### Métodos principais de `IPoolService`

```csharp
void EnsureRegistered(PoolDefinitionAsset definition);
void Prewarm(PoolDefinitionAsset definition);
GameObject Rent(PoolDefinitionAsset definition, Transform parent = null);
GameObject Spawn(PoolDefinitionAsset definition, Transform parent = null);
bool Return(PoolDefinitionAsset definition, GameObject instance);
int ReturnAll(PoolDefinitionAsset definition);
void Clear(PoolDefinitionAsset definition);
int ClearPoolsForScope(PoolLifetimeScope scope);
bool TryGetSnapshot(PoolDefinitionAsset definition, out PoolRuntimeSnapshot snapshot);
void Shutdown();
```

`Spawn` é alias de `Rent`.

## 6. `Registration Mode`

`PoolRegistrationMode` controla se o pool pode ser criado automaticamente no primeiro rent.

### `LazyOnFirstRent`

Permite:

```csharp
var instance = service.Rent(definition);
```

Mesmo que `EnsureRegistered` ainda não tenha sido chamado.

### `ExplicitPrepareOnly`

Exige:

```csharp
service.EnsureRegistered(definition);
// ou
service.Prewarm(definition);
```

Antes de `Rent`. Se tentar alugar sem preparar, o service lança erro dizendo que o pool requer registro explícito.

Use `ExplicitPrepareOnly` quando ausência de prewarm/configuração deve ser tratada como erro de authoring/integração.

## 7. Capacidade e expansão

Exemplo recomendado para objetos comuns:

```text
Initial Capacity = 8
Max Size = 32
Can Expand = true
```

Comportamento:

- `Prewarm` cria até `Initial Capacity`;
- `Rent` reutiliza instâncias inativas primeiro;
- se não houver instância inativa, cria nova se `Can Expand = true` e `TotalCount < MaxSize`;
- se atingir `MaxSize`, `Rent` lança erro de limite.

Exemplo para limite rígido:

```text
Initial Capacity = 4
Max Size = 4
Can Expand = false
```

Use para sistemas que não podem crescer dinamicamente, como orçamento fixo de vozes, projéteis ou efeitos controlados.

## 8. Callbacks em objetos pooláveis

Para reagir ao ciclo de vida do pool, herde de `PoolableBehaviour`:

```csharp
using Immersive.Pooling.Unity.Instances;
using UnityEngine;

public sealed class ProjectilePoolable : PoolableBehaviour
{
    protected override void HandleCreatedByPool()
    {
        // Chamado quando a instância é criada pelo pool.
    }

    protected override void HandleTakenFromPool()
    {
        // Chamado quando a instância é alugada/reutilizada.
        // Reative estado temporário aqui.
    }

    protected override void HandleReturnedToPool()
    {
        // Chamado antes da instância ser desativada.
        // Limpe estado temporário aqui.
    }

    protected override void HandleDestroyedByPool()
    {
        // Chamado quando o pool é limpo/destruído.
    }
}
```

`PoolableBehaviour` também expõe:

```csharp
bool IsTakenFromPool { get; }
int RentCount { get; }
```

Também é possível implementar diretamente:

```csharp
IPoolable
IPoolLifecycle
```

## 9. Devolver uma instância ao pool

Toda instância alugada recebe um `PoolReturnHandle` automaticamente.

Dentro do próprio prefab:

```csharp
using Immersive.Pooling.Unity.Instances;
using UnityEngine;

public sealed class ReturnSelfExample : MonoBehaviour
{
    public void ReturnSelf()
    {
        var handle = GetComponent<PoolReturnHandle>();
        if (handle != null)
        {
            handle.ReturnToPool();
        }
    }
}
```

`ReturnToPool()` retorna `false` quando:

- a instância não tem pool;
- a instância já foi retornada;
- o pool rejeitou o retorno;
- o objeto não pertence ao pool.

Retornos inválidos não devem corromper o estado interno.

## 10. Auto-return

Configure `Auto Return Seconds` no `PoolDefinitionAsset`.

Quando o valor é maior que `0`:

- cada `Rent` inicia um timer;
- se o objeto não for retornado manualmente antes, ele volta ao pool ao fim do tempo;
- se o objeto for retornado manualmente, o timer é cancelado;
- `Clear` cancela os timers ativos.

Uso típico:

```text
SFX pooled emitters
VFX curtos
Decals temporários
Objetos visuais descartáveis
```

Não use auto-return para objetos cujo lifetime dependa de lógica complexa de gameplay. Nesse caso, retorne manualmente.

## 11. Snapshots e contadores

Use `TryGetSnapshot` para UI, QA ou diagnóstico:

```csharp
if (service.TryGetSnapshot(definition, out var snapshot))
{
    Debug.Log($"active={snapshot.ActiveCount} inactive={snapshot.InactiveCount} total={snapshot.TotalCount}");
}
```

Campos:

```text
ActiveCount   = instâncias alugadas/ativas
InactiveCount = instâncias disponíveis no pool
TotalCount    = active + inactive
```

## 12. Limpeza

### `ReturnAll`

Retorna todas as instâncias ativas daquele pool:

```csharp
service.ReturnAll(definition);
```

### `Clear`

Destrói todas as instâncias daquele pool e remove o pool registrado:

```csharp
service.Clear(definition);
```

### `ClearPoolsForScope`

Remove todos os pools registrados com aquele `PoolLifetimeScope`:

```csharp
service.ClearPoolsForScope(PoolLifetimeScope.Temporary);
```

Importante: `PoolLifetimeScope` é apenas metadata do package. Ele não tem ligação automática com Route, Activity ou Framework.

### `Shutdown`

Finaliza o serviço inteiro:

```csharp
service.Shutdown();
```

Depois de `Shutdown`, chamadas no service lançam erro porque ele está encerrado.

## 13. QA Harness

O QA sintético criado para este package fica em:

```text
Assets/ImmersiveFrameworkQA/Pooling/
```

Gerador de cena:

```text
Immersive Framework QA > Pooling > Create or Refresh Pooling QA Scene
```

Cena criada:

```text
Assets/ImmersiveFrameworkQA/Pooling/Scenes/QA_Pooling.unity
```

Smokes disponíveis no painel:

```text
Basic Smoke
Max Limit Smoke
Auto Return Smoke
```

Critérios esperados:

- Basic Smoke: prewarm/rent/return/reuse/return all funciona;
- Max Limit Smoke: `maxSize` e `canExpand=false` são respeitados;
- Auto Return Smoke: objeto retorna automaticamente após o tempo configurado.

## 14. Uso futuro pelo Audio

Audio deve consumir pooling assim:

- manter referências explícitas a `PoolDefinitionAsset`;
- receber `IPoolService` por composição própria;
- não procurar serviço global;
- não depender de `FrameworkRuntimeHost`;
- usar pooled emitters apenas quando o pool estiver configurado.

Exemplo conceitual:

```csharp
public sealed class AudioSfxService
{
    private readonly IPoolService poolService;
    private readonly PoolDefinitionAsset emitterPool;

    public AudioSfxService(IPoolService poolService, PoolDefinitionAsset emitterPool)
    {
        this.poolService = poolService;
        this.emitterPool = emitterPool;
    }
}
```

## 15. Erros comuns

| Sintoma | Causa provável | Correção |
|---|---|---|
| `PoolDefinitionAsset requires a prefab` | Asset sem prefab. | Atribuir prefab válido. |
| `initialCapacity <= maxSize` falha | Capacidade inicial maior que limite. | Ajustar valores. |
| `canExpand=false` com `initialCapacity=0` | Pool rígido sem instâncias. | Usar `initialCapacity > 0`. |
| `Pool limit reached` | Todas as instâncias estão ativas e não pode criar mais. | Retornar objetos, aumentar `Max Size` ou habilitar `Can Expand`. |
| `requires explicit registration before rent` | `RegistrationMode = ExplicitPrepareOnly` sem `EnsureRegistered`/`Prewarm`. | Registrar/preparar antes de alugar. |
| `ReturnToPool` retorna `false` | Instância já retornada, sem handle, ou não pertence ao pool. | Validar origem e estado da instância. |

## 16. Checklist de integração

Antes de usar pooling em um sistema novo:

```text
[ ] Existe PoolDefinitionAsset com prefab válido.
[ ] Initial Capacity e Max Size fazem sentido.
[ ] Registration Mode foi escolhido conscientemente.
[ ] Objetos que precisam limpar estado usam PoolableBehaviour/IPoolable.
[ ] Retorno manual ou auto-return está definido.
[ ] Há dono explícito do PoolService ou PoolRuntimeHost.
[ ] O sistema não usa singleton/service locator.
[ ] Smoke básico passou.
```
