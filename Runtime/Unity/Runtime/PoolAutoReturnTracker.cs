using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Pooling.Unity.Runtime
{
    public sealed class PoolAutoReturnTracker
    {
        private readonly Dictionary<GameObject, Coroutine> _timers = new Dictionary<GameObject, Coroutine>();
        private readonly MonoBehaviour _coroutineHost;

        public PoolAutoReturnTracker(MonoBehaviour coroutineHost)
        {
            _coroutineHost = coroutineHost != null
                ? coroutineHost
                : throw new ArgumentNullException(nameof(coroutineHost));
        }

        public void Track(GameObject instance, float seconds, Func<GameObject, bool> returnToPool)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (returnToPool == null)
            {
                throw new ArgumentNullException(nameof(returnToPool));
            }

            Cancel(instance);

            if (seconds <= 0f)
            {
                return;
            }

            _timers[instance] = _coroutineHost.StartCoroutine(ReturnAfterDelay(instance, seconds, returnToPool));
        }

        public void Cancel(GameObject instance)
        {
            if (instance == null || !_timers.TryGetValue(instance, out var coroutine))
            {
                return;
            }

            if (coroutine != null)
            {
                _coroutineHost.StopCoroutine(coroutine);
            }

            _timers.Remove(instance);
        }

        public void Clear()
        {
            foreach (var coroutine in _timers.Values)
            {
                if (coroutine != null)
                {
                    _coroutineHost.StopCoroutine(coroutine);
                }
            }

            _timers.Clear();
        }

        private IEnumerator ReturnAfterDelay(GameObject instance, float seconds, Func<GameObject, bool> returnToPool)
        {
            yield return new WaitForSeconds(seconds);
            _timers.Remove(instance);

            if (instance != null)
            {
                returnToPool(instance);
            }
        }
    }
}
