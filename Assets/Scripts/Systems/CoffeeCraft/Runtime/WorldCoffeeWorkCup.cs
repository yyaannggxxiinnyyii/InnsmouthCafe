using System;
using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>可在咖啡制作工位之间拖拽并吸附的实体工作杯。</summary>
    [RequireComponent(typeof(Collider))]
    public class WorldCoffeeWorkCup : MonoBehaviour
    {
        [Serializable]
        private class CupSideProfile
        {
            [Tooltip("必须与杯型 SO 的 cupId 完全一致")]
            public string cupId;

            [Tooltip("该杯型侧剖面的根对象")]
            public GameObject profileRoot;

            [Tooltip("该杯型侧剖面的杯子贴图")]
            public SpriteRenderer cupRenderer;

            [Tooltip("该杯型的杯内粒子检测区域")]
            public Collider2D collectionArea;

            [Tooltip("该杯型侧剖面的 LiquidFun LPBody，SpawnOnPlay 必须关闭")]
            public LPBody liquidBody;
        }

        [Header("视觉组件")]
        [SerializeField] private SpriteRenderer _cupRenderer;

        [Header("杯型侧剖面")]
        [Tooltip("按 cupId 选择对应的贴图、检测区域和 LiquidFun 物理结构")]
        [SerializeField] private CupSideProfile[] _cupSideProfiles;

        [Header("容量组件")]
        [Tooltip("用于读取杯内粒子并计算容量的组件")]
        [SerializeField] private CoffeeCupLiquidVolume _liquidVolume;

        private Camera _camera;
        private LPManager _liquidManager;
        private bool _dragging;
        private float _dragPlaneY;
        private Vector3 _lastValidPosition;
        private CoffeeCupAnchor _currentAnchor;
        private CoffeeCupAnchor _lastValidAnchor;
        private CupContainerData _cupData;
        private CupSideProfile _activeProfile;
        private bool _interactionLocked;

        /// <summary>工作杯当前吸附的制作工位锚点。</summary>
        public CoffeeCupAnchor CurrentAnchor => _currentAnchor;

        /// <summary>工作杯吸附位置发生变化时触发。</summary>
        public event Action<CoffeeCupAnchor> OnAnchorChanged;

        private void Awake()
        {
            _camera = Camera.main;
            _liquidManager = FindObjectOfType<LPManager>();
            _lastValidPosition = transform.position;

            if (_cupRenderer == null)
            {
                _cupRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_liquidVolume == null)
            {
                _liquidVolume = GetComponentInChildren<CoffeeCupLiquidVolume>();
            }

            if (_liquidVolume == null)
            {
                _liquidVolume = FindObjectOfType<CoffeeCupLiquidVolume>();
            }

            DisableAllSideProfiles();
        }

        /// <summary>在所有 LiquidFun 对象完成 Awake 后，清理可能被提前初始化的侧剖面 Body。</summary>
        private void Start()
        {
            _liquidManager = _liquidManager != null ? _liquidManager : FindObjectOfType<LPManager>();
            PrepareSideProfiles();
        }

        /// <summary>设置工作杯显示的杯型数据并吸附到指定工位。</summary>
        public void SetCup(CupContainerData cup, CoffeeCupAnchor anchor)
        {
            if (cup == null || anchor == null)
            {
                Debug.LogWarning("[WorldCoffeeWorkCup] 杯型数据或托盘锚点为空，无法设置工作杯。", this);
                return;
            }

            _cupData = cup;
            SetCupSprite(cup, null);
            SnapToAnchor(anchor);
            _lastValidPosition = transform.position;
            gameObject.SetActive(true);
        }

        /// <summary>开始拖拽工作杯。</summary>
        public bool BeginDrag()
        {
            if (_interactionLocked)
            {
                return false;
            }

            if (_camera == null) _camera = Camera.main;
            _dragging = _camera != null;
            _dragPlaneY = transform.position.y;
            _lastValidPosition = transform.position;
            _lastValidAnchor = _currentAnchor;
            SyncYawToCamera();
            SetCurrentAnchor(null);
            return _dragging;
        }

        /// <summary>根据鼠标位置更新工作杯位置。</summary>
        public void Drag()
        {
            if (!_dragging || _camera == null) return;
            SyncYawToCamera();
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, _dragPlaneY, 0f));
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 target = ray.GetPoint(distance);
                transform.position = new Vector3(target.x, _dragPlaneY, target.z);
            }
        }

        /// <summary>结束拖拽，吸附到最近锚点或返回上次有效位置。</summary>
        public void EndDrag()
        {
            if (!_dragging) return;
            _dragging = false;
            if (TryDiscardAtTrashBin()) return;

            CoffeeCupAnchor nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (CoffeeCupAnchor anchor in FindObjectsOfType<CoffeeCupAnchor>())
            {
                if (anchor == null) continue;
                float distance = Vector3.Distance(transform.position, anchor.transform.position);
                if (distance <= anchor.SnapRadius && distance < nearestDistance)
                {
                    nearest = anchor;
                    nearestDistance = distance;
                }
            }

            if (nearest != null) SnapToAnchor(nearest);
            else if (_lastValidAnchor != null) SnapToAnchor(_lastValidAnchor);
            else
            {
                transform.position = _lastValidPosition;
                SetCurrentAnchor(_lastValidAnchor);
            }
        }

        /// <summary>判断工作杯是否吸附在指定工位。</summary>
        public bool IsAtAnchor(CoffeeCupAnchor anchor)
        {
            return anchor != null && _currentAnchor == anchor;
        }

        /// <summary>根据容量比例切换当前杯型的填充贴图。</summary>
        public void SetFillRatio(float fillRatio)
        {
            if (_cupData == null) return;
            SetCupSprite(_cupData, _activeProfile, fillRatio);
        }

        /// <summary>设置工作杯是否禁止拖拽。</summary>
        public void SetInteractionLocked(bool locked)
        {
            _interactionLocked = locked;
        }

        /// <summary>根据制作流程中的杯型数据激活对应侧剖面并初始化 LiquidFun LPBody。</summary>
        public bool ActivateCupProfile(CupContainerData cupData)
        {
            if (cupData == null)
            {
                Debug.LogWarning("[WorldCoffeeWorkCup] 制作流程中没有杯型数据，无法激活杯型侧剖面。", this);
                return false;
            }

            CupSideProfile profile = FindSideProfile(cupData.cupId);
            if (profile == null)
            {
                Debug.LogWarning("[WorldCoffeeWorkCup] 未找到当前杯型对应的侧剖面：" + cupData.cupId, this);
                return false;
            }

            SwitchSideProfile(profile);
            SetCupProfileVisualActive(true);
            _cupData = cupData;
            _liquidVolume?.Configure(cupData, profile.collectionArea);
            return true;
        }

        /// <summary>关闭当前杯型侧剖面并释放其 LiquidFun 原生 Body。</summary>
        public void DeactivateSelectedCupProfile()
        {
            DeleteActiveLiquidBody();
            if (_activeProfile?.profileRoot != null)
            {
                _activeProfile.profileRoot.SetActive(false);
            }

            _activeProfile = null;
        }

        /// <summary>
        /// 只切换当前侧剖面的视觉显示，不影响杯内碰撞和粒子统计。
        /// </summary>
        public void SetCupProfileVisualActive(bool isActive)
        {
            if (_activeProfile?.profileRoot == null)
            {
                return;
            }

            Renderer[] renderers = _activeProfile.profileRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = isActive;
            }
        }

        /// <summary>按杯型 ID 查找对应的侧剖面配置。</summary>
        private CupSideProfile FindSideProfile(string cupId)
        {
            if (_cupSideProfiles == null) return null;
            foreach (CupSideProfile profile in _cupSideProfiles)
            {
                if (profile != null && profile.cupId == cupId) return profile;
            }
            return null;
        }

        /// <summary>关闭旧侧剖面并初始化新的 LiquidFun 物理结构。</summary>
        private void SwitchSideProfile(CupSideProfile profile)
        {
            if (_activeProfile == profile) return;
            DeleteActiveLiquidBody();
            DisableAllSideProfiles();
            _activeProfile = profile;

            if (profile.profileRoot != null) profile.profileRoot.SetActive(true);
            if (profile.liquidBody == null) return;
            if (_liquidManager == null) _liquidManager = FindObjectOfType<LPManager>();
            if (_liquidManager == null)
            {
                Debug.LogError("[WorldCoffeeWorkCup] 场景中没有 LPManager，无法初始化杯型物理结构。", this);
                return;
            }

            if (!profile.liquidBody.Initialised)
            {
                profile.liquidBody.Initialise(_liquidManager);
            }
        }

        /// <summary>删除当前侧剖面的原生 Body，但保留 Unity 组件以便下次复用。</summary>
        private void DeleteActiveLiquidBody()
        {
            if (_activeProfile?.liquidBody == null || !_activeProfile.liquidBody.Initialised) return;
            _activeProfile.liquidBody.DeleteWithoutRemovingComponents();
            _activeProfile.liquidBody.Initialised = false;
        }

        /// <summary>关闭所有侧剖面根对象，避免多个杯型同时参与碰撞。</summary>
        private void DisableAllSideProfiles()
        {
            if (_cupSideProfiles == null) return;
            foreach (CupSideProfile profile in _cupSideProfiles)
            {
                if (profile?.profileRoot != null) profile.profileRoot.SetActive(false);
            }
        }

        /// <summary>禁止侧剖面自动生成 Body，并清理已被管理器提前生成的 Body。</summary>
        private void PrepareSideProfiles()
        {
            if (_cupSideProfiles == null) return;
            foreach (CupSideProfile profile in _cupSideProfiles)
            {
                if (profile?.liquidBody == null) continue;
                profile.liquidBody.SpawnOnPlay = false;
                if (profile.liquidBody.Initialised)
                {
                    profile.liquidBody.DeleteWithoutRemovingComponents();
                    profile.liquidBody.Initialised = false;
                }
            }
        }

        /// <summary>设置当前侧剖面的填充贴图。</summary>
        private void SetCupSprite(CupContainerData cup, CupSideProfile profile, float ratio = 0f)
        {
            SpriteRenderer renderer = profile?.cupRenderer != null ? profile.cupRenderer : _cupRenderer;
            if (renderer != null) renderer.sprite = cup.GetSpriteForFillRatio(Mathf.Clamp01(ratio));
        }

        /// <summary>判断工作杯是否进入垃圾桶范围，并执行垃圾桶交互。</summary>
        private bool TryDiscardAtTrashBin()
        {
            foreach (WorldCoffeeTrashBinInteractable trashBin in FindObjectsOfType<WorldCoffeeTrashBinInteractable>())
            {
                if (trashBin != null && trashBin.IsCupWithinDiscardRange(transform.position)) return trashBin.TryDiscardCup();
            }
            return false;
        }

        /// <summary>将工作杯对齐到锚点并记录有效位置。</summary>
        private void SnapToAnchor(CoffeeCupAnchor anchor)
        {
            transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation);
            _lastValidPosition = transform.position;
            _lastValidAnchor = anchor;
            SetCurrentAnchor(anchor);
        }

        /// <summary>拖拽时让工作杯绕 Y 轴朝向当前摄像机。</summary>
        private void SyncYawToCamera()
        {
            if (!_dragging || _camera == null) return;
            Vector3 rotation = transform.eulerAngles;
            rotation.y = _camera.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(rotation);
        }

        /// <summary>更新当前锚点并通知外部系统。</summary>
        private void SetCurrentAnchor(CoffeeCupAnchor anchor)
        {
            if (_currentAnchor == anchor) return;
            _currentAnchor = anchor;
            OnAnchorChanged?.Invoke(_currentAnchor);
        }
    }
}
