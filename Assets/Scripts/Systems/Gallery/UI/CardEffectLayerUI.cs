using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 管理单个卡牌特效覆盖层的运行时材质，并接收统一卡牌光照参数。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public class CardEffectLayerUI : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField]
        [Tooltip("承载特效材质的覆盖层 Image；为空时自动获取当前节点的 Image")]
        private Image _overlayImage;

        [SerializeField]
        [Tooltip("所属卡牌的统一特效控制器；为空时自动在父级查找")]
        private CardEffectControllerUI _effectController;

        private Material _sourceMaterial;
        private Material _runtimeMaterial;
        private Material _rejectedMaterial;

        protected virtual void Awake()
        {
            _overlayImage ??= GetComponent<Image>();
            _effectController ??= GetComponentInParent<CardEffectControllerUI>();
            EnsureRuntimeMaterial();
        }

        protected virtual void Update()
        {
            if (_effectController == null || !EnsureRuntimeMaterial())
            {
                return;
            }

            SynchronizeSourceMaterialProperties();
            _effectController.ApplyToMaterial(_runtimeMaterial);
        }

        protected virtual void OnDestroy()
        {
            ReleaseRuntimeMaterial(true);
        }

        /// <summary>
        /// 判断当前材质是否适用于该特效层；通用层默认接受所有有效 Shader。
        /// </summary>
        protected virtual bool IsMaterialSupported(Material material)
        {
            return material != null && material.shader != null;
        }

        /// <summary>
        /// 返回材质不受支持时输出的警告内容。
        /// </summary>
        protected virtual string GetUnsupportedMaterialWarning()
        {
            return $"[Gallery] {name} 的特效层材质无效，无法同步卡牌光照参数";
        }

        /// <summary>
        /// 检测覆盖层材质是否被替换，并确保 Image 使用当前源材质的独立运行时副本。
        /// </summary>
        private bool EnsureRuntimeMaterial()
        {
            if (_overlayImage == null)
            {
                return false;
            }

            Material assignedMaterial = _overlayImage.material;
            if (_runtimeMaterial != null && assignedMaterial == _runtimeMaterial)
            {
                return true;
            }

            ReleaseRuntimeMaterial(false);
            _sourceMaterial = assignedMaterial;
            if (!IsMaterialSupported(_sourceMaterial))
            {
                if (_sourceMaterial != null && _rejectedMaterial != _sourceMaterial)
                {
                    Debug.LogWarning(GetUnsupportedMaterialWarning(), this);
                    _rejectedMaterial = _sourceMaterial;
                }

                return false;
            }

            _rejectedMaterial = null;
            _runtimeMaterial = new Material(_sourceMaterial)
            {
                name = $"{_sourceMaterial.name} ({name} Runtime)"
            };
            _overlayImage.material = _runtimeMaterial;
            return true;
        }

        /// <summary>
        /// 在编辑器播放状态下同步源材质参数，支持边调材质边观察动态效果。
        /// </summary>
        private void SynchronizeSourceMaterialProperties()
        {
#if UNITY_EDITOR
            if (_sourceMaterial != null && _runtimeMaterial != null)
            {
                _runtimeMaterial.CopyPropertiesFromMaterial(_sourceMaterial);
            }
#endif
        }

        /// <summary>
        /// 销毁当前运行时材质，并按需将 Image 恢复为源材质。
        /// </summary>
        private void ReleaseRuntimeMaterial(bool restoreSourceMaterial)
        {
            if (_runtimeMaterial == null)
            {
                return;
            }

            if (restoreSourceMaterial
                && _overlayImage != null
                && _overlayImage.material == _runtimeMaterial)
            {
                _overlayImage.material = _sourceMaterial;
            }

            Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }
}
