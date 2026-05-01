using System.Collections.Generic;
using UnityEngine;

namespace Game.Framework.Pooling
{
    /// <summary>
    /// 泛型对象池。以某个 Component 原型为键 —— 这是 Unity 里最方便的单位：
    /// 子弹 Prefab 上挂 Bullet 脚本，PoolManager.Get&lt;Bullet&gt;(bulletPrefab) 直接返回脚本引用。
    ///
    /// 线程：主线程独占。
    /// GC：泛型 Stack 会为每个 T 分一份，正常使用量下可忽略。
    /// </summary>
    public class ObjectPool<T> : IPoolClearable where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _idle;
        private readonly int _maxSize;

        public int CountIdle => _idle.Count;

        public ObjectPool(T prefab, Transform parent, int prewarm = 0, int maxSize = 256)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            _idle = new Stack<T>(Mathf.Max(8, prewarm));
            for (int i = 0; i < prewarm; i++)
            {
                var inst = CreateInstance();
                inst.gameObject.SetActive(false);
                _idle.Push(inst);
            }
        }

        public T Acquire(Vector3 position, Quaternion rotation)
        {
            T inst = null;
            // Pop 出的对象可能已被外部 Destroy（Unity 的 "fake null"），循环跳过直到拿到活的，否则新建
            while (_idle.Count > 0)
            {
                var candidate = _idle.Pop();
                if (candidate != null) { inst = candidate; break; }
            }
            if (inst == null) inst = CreateInstance();

            var tr = inst.transform;
            tr.SetPositionAndRotation(position, rotation);
            inst.gameObject.SetActive(true);

            if (inst is IPoolable p) p.OnAcquire();
            return inst;
        }

        public void Release(T inst)
        {
            if (inst == null) return;
            if (inst is IPoolable p) p.OnRelease();

            inst.gameObject.SetActive(false);
            inst.transform.SetParent(_parent, worldPositionStays: false);

            if (_idle.Count >= _maxSize)
            {
                Object.Destroy(inst.gameObject); // 超池上限就别存了
                return;
            }
            _idle.Push(inst);
        }

        private T CreateInstance()
        {
            var inst = Object.Instantiate(_prefab, _parent);
            return inst;
        }

        public void Clear()
        {
            while (_idle.Count > 0)
            {
                var inst = _idle.Pop();
                if (inst != null) Object.Destroy(inst.gameObject);
            }
        }
    }
}
