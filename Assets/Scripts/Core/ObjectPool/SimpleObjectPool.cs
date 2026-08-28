using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Core
{
    /// <summary>
    /// 简单对象池（GameObject 专用）。
    /// 支持从池中获取、回收、批量清理。
    /// </summary>
    public class SimpleObjectPool
    {
        private readonly Dictionary<GameObject, Stack<GameObject>> _pools = new Dictionary<GameObject, Stack<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> _activePrefabMap = new Dictionary<GameObject, GameObject>();
        private readonly Transform _poolRoot;

        public SimpleObjectPool(Transform poolRoot = null)
        {
            _poolRoot = poolRoot;
        }

        /// <summary>
        /// 从池中获取或创建新对象。
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            // 确保池存在
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Stack<GameObject>();
            }

            GameObject obj;

            // 从池中取
            if (_pools[prefab].Count > 0)
            {
                obj = _pools[prefab].Pop();
                obj.transform.SetParent(parent);
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
            // 池空，创建新的
            else
            {
                obj = Object.Instantiate(prefab, position, rotation, parent);
            }

            // 记录映射（用于回收时知道属于哪个 prefab 池）
            _activePrefabMap[obj] = prefab;

            return obj;
        }

        /// <summary>
        /// 回收对象到池。
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            // 查找对应的 prefab 池
            if (_activePrefabMap.TryGetValue(obj, out GameObject prefab))
            {
                obj.SetActive(false);
                obj.transform.SetParent(_poolRoot);
                _pools[prefab].Push(obj);
                _activePrefabMap.Remove(obj);
            }
            else
            {
                // 找不到映射，直接销毁
                Object.Destroy(obj);
            }
        }

        /// <summary>
        /// 回收所有活动对象。
        /// </summary>
        public void ReturnAll()
        {
            // 复制键列表（避免遍历时修改字典）
            List<GameObject> activeObjects = new List<GameObject>(_activePrefabMap.Keys);

            foreach (var obj in activeObjects)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// 清空池并销毁所有对象。
        /// </summary>
        public void Clear()
        {
            // 回收所有活动对象
            ReturnAll();

            // 销毁池中所有对象
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Pop();
                    if (obj != null) Object.Destroy(obj);
                }
            }

            _pools.Clear();
            _activePrefabMap.Clear();
        }

        /// <summary>
        /// 获取池统计信息（调试用）。
        /// </summary>
        public string GetStats()
        {
            int totalPooled = 0;
            foreach (var pool in _pools.Values)
            {
                totalPooled += pool.Count;
            }

            return $"Active: {_activePrefabMap.Count}, Pooled: {totalPooled}, Total Prefabs: {_pools.Count}";
        }
    }
}
