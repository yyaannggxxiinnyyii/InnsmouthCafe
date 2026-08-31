using System;
using UnityEngine;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 辅助液加注原型的场景交互控制器，负责拖动倾倒瓶子、驱动液滴发射并统计杯内液滴数量。
    /// </summary>
    public class LiquidPouringPrototypeController : MonoBehaviour
    {
        [Header("场景引用")]
        [SerializeField] private Camera _prototypeCamera;
        [SerializeField] private Transform _bottleTransform;
        [Tooltip("倾倒瓶下方用于显示当前辅助液图标的贴图组件")]
        [SerializeField] private SpriteRenderer _bottleLiquidIconRenderer;
        [SerializeField] private Collider2D _bottleInputCollider;
        [SerializeField] private LPParticleSpawner _liquidSpawner;
        [SerializeField] private LPParticleSystem _liquidParticleSystem;
        [SerializeField] private Collider2D _cupCollectionArea;

        [Header("空格倾倒")]
        [Tooltip("按住空格时瓶子的最大倾斜角度")]
        [SerializeField] private Transform _bottleRotationPivot;

        [Tooltip("瓶子允许的最大倾斜角度")]
        [SerializeField] private float _maximumTiltAngle = 80f;
        [SerializeField] private float _tiltSpeed = 90f;
        [SerializeField] private float _returnSpeed = 180f;

        [Header("倾倒速度")]
        [Tooltip("刚进入允许倾倒方向时，每秒生成的液滴数量")]
        [SerializeField] private float _minimumPourRate = 12f;

        [Tooltip("达到最大倾倒角度时，每秒生成的液滴数量")]
        [SerializeField] private float _maximumPourRate = 90f;

        [Tooltip("连线相对水平线达到该角度时使用最大倾倒速度")]
        [SerializeField] private float _maximumPourAngle = 90f;

        private bool _isPouring;
        private Quaternion _bottleStartRotation;
        private Vector3 _bottleStartPosition;
        private Vector3 _rotationPivotWorldPosition;
        private float _currentTiltAngle;
        private int _collectedParticleCount;
        private LPDrawParticleSystem _particleRenderer;
        private bool _liquidAddActive;

        /// <summary>
        /// 当前位于杯内收集区域的液滴数量。
        /// </summary>
        public int CollectedParticleCount => _collectedParticleCount;

        /// <summary>
        /// 当前倒液控制器使用的 LiquidFun 粒子系统。
        /// </summary>
        public LPParticleSystem LiquidParticleSystem => _liquidParticleSystem;

        /// <summary>
        /// 获取用于点击退出加液面板的倾倒瓶专用碰撞体。
        /// </summary>
        public Collider2D BottleInputCollider => _bottleInputCollider;

        /// <summary>
        /// 设置倾倒瓶下方显示的当前辅助液图标。
        /// </summary>
        /// <param name="icon">辅助液配置中的图标；传入空值时隐藏图标。</param>
        public void SetLiquidIcon(Sprite icon)
        {
            if (_bottleLiquidIconRenderer == null)
            {
                return;
            }

            _bottleLiquidIconRenderer.sprite = icon;
            _bottleLiquidIconRenderer.enabled = icon != null;
        }

        /// <summary>
        /// 杯内液滴数量变化时触发。
        /// </summary>
        public event Action<int> OnCollectedParticleCountChanged;

        private void Awake()
        {
            if (_prototypeCamera == null)
            {
                _prototypeCamera = Camera.main;
            }
        }

        private void Start()
        {
            _particleRenderer = _liquidParticleSystem != null
                ? _liquidParticleSystem.GetComponentInChildren<LPDrawParticleSystem>(true)
                : null;
            if (_bottleTransform != null)
            {
                _bottleStartPosition = _bottleTransform.position;
                _bottleStartRotation = _bottleTransform.rotation;
                _rotationPivotWorldPosition = _bottleRotationPivot != null
                    ? _bottleRotationPivot.position
                    : _bottleTransform.position;
            }

            if (_liquidSpawner != null)
            {
                _liquidSpawner.Active = false;
                _liquidSpawner.SpawnsPerSecond = Mathf.Max(0.1f, _minimumPourRate);
                _liquidSpawner.StopSpawning();
            }
            SetLiquidAddActive(false);
        }

        private void Update()
        {
            if (!_liquidAddActive)
            {
                return;
            }

            HandleBottleInput();
            RefreshCollectedParticleCount();
        }

        private void OnDisable()
        {
            StopPouring();
        }

        /// <summary>
        /// 重置倾倒控制器的运行状态，供垃圾桶清理当前制作时调用。
        /// </summary>
        public void ResetForDiscard()
        {
            StopPouring();
            _collectedParticleCount = 0;
            SetLiquidAddActive(false);
        }

        /// <summary>切换加液阶段的瓶子、粒子显示和输入状态，不影响 LiquidFun 核心系统。</summary>
        public void SetLiquidAddActive(bool isActive)
        {
            _liquidAddActive = isActive;
            if (!isActive)
            {
                StopPouring();
                ResetBottleRotation();
            }

            if (_bottleTransform != null && _bottleTransform.gameObject.activeSelf != isActive)
            {
                _bottleTransform.gameObject.SetActive(isActive);
            }

            if (_particleRenderer != null && _particleRenderer.gameObject.activeSelf != isActive)
            {
                _particleRenderer.gameObject.SetActive(isActive);
            }
        }

        /// <summary>
        /// 处理空格倾斜输入，并在松开后平滑回到初始角度。
        /// </summary>
        private void HandleBottleInput()
        {
            if (_bottleTransform == null)
            {
                return;
            }

            if (Input.GetKey(KeyCode.Space))
            {
                _currentTiltAngle = Mathf.MoveTowards(
                    _currentTiltAngle, _maximumTiltAngle, Mathf.Max(0f, _tiltSpeed) * Time.deltaTime);
            }
            else
            {
                _currentTiltAngle = Mathf.MoveTowards(
                    _currentTiltAngle, 0f, Mathf.Max(0f, _returnSpeed) * Time.deltaTime);
            }

            ApplyBottleRotation();
            if (Input.GetKey(KeyCode.Space) && IsNozzleInPouringQuadrant())
            {
                UpdatePourSpeed();
                StartPouring();
            }
            else
            {
                StopPouring();
            }
        }

        /// <summary>
        /// 将倾倒瓶恢复到初始位置和角度。
        /// </summary>
        private void ResetBottleRotation()
        {
            _currentTiltAngle = 0f;
            if (_bottleTransform != null)
            {
                _bottleTransform.SetPositionAndRotation(_bottleStartPosition, _bottleStartRotation);
            }
        }

        /// <summary>
        /// 按旋转锚点更新倾倒瓶的世界位置和角度，确保瓶子绕锚点旋转。
        /// </summary>
        private void ApplyBottleRotation()
        {
            if (_bottleTransform == null)
            {
                return;
            }

            Quaternion rotationDelta = Quaternion.Euler(0f, 0f, _currentTiltAngle);
            _bottleTransform.position = _rotationPivotWorldPosition
                + rotationDelta * (_bottleStartPosition - _rotationPivotWorldPosition);
            _bottleTransform.rotation = rotationDelta * _bottleStartRotation;
        }

        /// <summary>
        /// 启动瓶口对应的 LiquidFun 单粒子发射器。
        /// </summary>
        private void StartPouring()
        {
            if (_isPouring || _liquidSpawner == null)
            {
                return;
            }

            _liquidSpawner.StartSpawning();
            _isPouring = true;
        }

        /// <summary>
        /// 停止当前瓶口的 LiquidFun 液滴发射。
        /// </summary>
        private void StopPouring()
        {
            if (!_isPouring || _liquidSpawner == null)
            {
                return;
            }

            _liquidSpawner.StopSpawning();
            _liquidSpawner.SpawnsPerSecond = Mathf.Max(0.1f, _minimumPourRate);
            _isPouring = false;
        }

        /// <summary>
        /// 根据发射器与目标点连线的倾倒角度更新液滴生成速度。
        /// </summary>
        private void UpdatePourSpeed()
        {
            if (_liquidSpawner == null || _liquidSpawner.target == null)
            {
                return;
            }

            Vector2 spawnerOffset = _liquidSpawner.transform.position - _liquidSpawner.target.transform.position;
            float pourAngle = Mathf.Atan2(spawnerOffset.y, spawnerOffset.x) * Mathf.Rad2Deg;
            float maximumAngle = Mathf.Max(0.1f, _maximumPourAngle);
            float normalizedAngle = Mathf.Clamp01(pourAngle / maximumAngle);
            float minimumRate = Mathf.Max(0.1f, _minimumPourRate);
            float maximumRate = Mathf.Max(minimumRate, _maximumPourRate);
            float pourRate = Mathf.Lerp(minimumRate, maximumRate, normalizedAngle);
            _liquidSpawner.SpawnsPerSecond = Mathf.Max(0.1f, pourRate);
        }

        /// <summary>
        /// 判断瓶口发射器是否位于目标点的第一象限，仅在该朝向下允许开始倾倒。
        /// </summary>
        private bool IsNozzleInPouringQuadrant()
        {
            if (_liquidSpawner == null || _liquidSpawner.target == null)
            {
                return false;
            }

            Vector2 spawnerOffset = _liquidSpawner.transform.position - _liquidSpawner.target.transform.position;
            return spawnerOffset.x > 0f && spawnerOffset.y > 0f;
        }

        /// <summary>
        /// 获取当前瓶子旋转锚点的世界坐标。
        /// </summary>
        private Vector3 GetRotationPivotWorldPosition()
        {
            return _bottleRotationPivot != null
                ? _bottleRotationPivot.position
                : _bottleTransform.position;
        }

        /// <summary>
        /// 获取鼠标相对旋转锚点的世界角度。
        /// </summary>
        private float GetPointerAngle(Vector3 pivotWorldPosition)
        {
            Vector2 pointerOffset = GetPointerWorldPosition() - (Vector2)pivotWorldPosition;
            return Mathf.Atan2(pointerOffset.y, pointerOffset.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 统计当前停留在杯内收集区域的 LiquidFun 液滴数量。
        /// </summary>
        private void RefreshCollectedParticleCount()
        {
            if (_liquidParticleSystem == null || _liquidParticleSystem.Particles == null || _cupCollectionArea == null)
            {
                return;
            }

            int particleCount = 0;
            foreach (LPParticle particle in _liquidParticleSystem.Particles)
            {
                if (_cupCollectionArea.OverlapPoint(particle.Position))
                {
                    particleCount++;
                }
            }

            if (_collectedParticleCount == particleCount)
            {
                return;
            }

            _collectedParticleCount = particleCount;
            OnCollectedParticleCountChanged?.Invoke(_collectedParticleCount);
        }

        /// <summary>
        /// 将屏幕鼠标坐标转换为原型场景使用的世界坐标。
        /// </summary>
        private Vector2 GetPointerWorldPosition()
        {
            Ray pointerRay = _prototypeCamera.ScreenPointToRay(Input.mousePosition);
            Plane bottlePlane = new Plane(
                Vector3.forward,
                new Vector3(0f, 0f, _bottleTransform.position.z));

            if (bottlePlane.Raycast(pointerRay, out float distance) && distance >= 0f)
            {
                return pointerRay.GetPoint(distance);
            }

            Vector3 screenPosition = Input.mousePosition;
            screenPosition.z = Mathf.Abs(_prototypeCamera.transform.position.z - _bottleTransform.position.z);
            return _prototypeCamera.ScreenToWorldPoint(screenPosition);
        }
    }
}
