using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework.Pooling
{
    /// <summary>
    /// 全局对象池管家。按 (Type, prefab) 双键索引，保证同一个 Prefab 不会因为泛型参数不同而被装两次。
    ///
    /// 用法：
    ///   var bullet = PoolManager.Get&lt;Bullet&gt;(bulletPrefab).Acquire(pos, rot);
    ///   ...
    ///   PoolManager.Get&lt;Bullet&gt;(bulletPrefab).Release(bullet);
    ///
    /// 或者（更常见）让 Bullet 自己在命中后调用 PoolManager.Release(this)。
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager _instance;

        public static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("~PoolManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PoolManager>();
                }
                return _instance;
            }
        }

        // key = (componentType, prefabInstanceID)，value = 弱类型 object（即 ObjectPool<T>）
        private readonly Dictionary<(Type, int), object> _pools = new Dictionary<(Type, int), object>();

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                foreach (var kv in _pools)
                    if (kv.Value is IPoolClearable pc) pc.Clear();
                _pools.Clear();
                _instance = null;
            }
        }

        public static ObjectPool<T> Get<T>(T prefab, int prewarm = 0, int maxSize = 256) where T : Component
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var mgr = Instance;
            var key = (typeof(T), prefab.GetInstanceID());
            if (!mgr._pools.TryGetValue(key, out var obj))
            {
                var parent = new GameObject($"Pool<{typeof(T).Name}>#{prefab.name}").transform;
                parent.SetParent(mgr.transform);
                var pool = new ObjectPool<T>(prefab, parent, prewarm, maxSize);
                mgr._pools[key] = pool;
                return pool;
            }
            return (ObjectPool<T>)obj;
        }
    }

    // 标记接口，仅给 PoolManager 统一清理调用 —— ObjectPool<T> 不是 MB，无法走 OnDestroy
    // 放成 public 以避免 public ObjectPool<T> 实现 internal 接口引发的可见性嘈音
    public interface IPoolClearable { void Clear(); }
}
