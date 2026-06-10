using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// UI背景高斯模糊组件
    /// 截取当前屏幕画面，经过两次Pass模糊后贴到RawImage上
    /// 用法：挂在需要模糊背景的面板根节点，面板显示时调用 Capture()，隐藏时调用 Release()
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class UIBlurBackground : MonoBehaviour
    {
        [Header("模糊配置")]
        [SerializeField] [Tooltip("高斯模糊材质球（使用 InnsmouthCafe/GaussianBlur shader）")]
        private Material _blurMaterial;

        [SerializeField] [Tooltip("模糊强度（对应 Shader 的 BlurSize）")]
        [Range(0f, 10f)]
        private float _blurSize = 2f;

        [SerializeField] [Tooltip("降采样倍数（越大性能越好，模糊越粗糙，建议2或4）")]
        [Range(1, 8)]
        private int _downSample = 2;

        [SerializeField] [Tooltip("迭代次数（越多越模糊，性能消耗越高，建议1~3）")]
        [Range(1, 5)]
        private int _iterations = 2;

        private RawImage _rawImage;
        private RenderTexture _blurredRT;
        private Coroutine _captureCoroutine;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _rawImage.enabled = false;
        }

        private void OnDestroy()
        {
            Release();
        }

        /// <summary>
        /// 截取当前屏幕并生成模糊贴图，显示到 RawImage
        /// 在面板打开时调用（等待一帧后截图，避免截到面板本身）
        /// </summary>
        public void Capture()
        {
            if (_blurMaterial == null)
            {
                Debug.LogWarning("[UIBlurBackground] 未指定模糊材质球");
                return;
            }

            if (_captureCoroutine != null)
                StopCoroutine(_captureCoroutine);

            _captureCoroutine = StartCoroutine(CaptureCoroutine());
        }

        /// <summary>
        /// 释放模糊贴图，隐藏 RawImage
        /// 在面板关闭后调用
        /// </summary>
        public void Release()
        {
            if (_captureCoroutine != null)
            {
                StopCoroutine(_captureCoroutine);
                _captureCoroutine = null;
            }

            if (_rawImage != null)
                _rawImage.enabled = false;

            ReleaseRT();
        }

        private IEnumerator CaptureCoroutine()
        {
            // 先隐藏自身，等渲染完这一帧再截图，避免截到模糊层本身
            _rawImage.enabled = false;
            yield return new WaitForEndOfFrame();

            int w = Screen.width  / _downSample;
            int h = Screen.height / _downSample;

            ReleaseRT();

            // WaitForEndOfFrame 之后直接 ReadPixels 读屏幕帧缓冲
            Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenTex.Apply();

            // 降采样 + 迭代模糊
            RenderTexture rt0 = RenderTexture.GetTemporary(w, h, 0);
            RenderTexture rt1 = RenderTexture.GetTemporary(w, h, 0);
            Graphics.Blit(screenTex, rt0);

            Destroy(screenTex);

            _blurMaterial.SetFloat("_BlurSize", _blurSize);
            for (int i = 0; i < _iterations; i++)
            {
                Graphics.Blit(rt0, rt1, _blurMaterial, 0); // 水平 Pass
                Graphics.Blit(rt1, rt0, _blurMaterial, 1); // 垂直 Pass
            }

            RenderTexture.ReleaseTemporary(rt1);

            // 保存最终结果到持久 RT
            _blurredRT = new RenderTexture(w, h, 0);
            Graphics.Blit(rt0, _blurredRT);
            RenderTexture.ReleaseTemporary(rt0);

            _rawImage.texture = _blurredRT;
            _rawImage.enabled = true;
            _captureCoroutine = null;
        }

        private void ReleaseRT()
        {
            if (_blurredRT != null)
            {
                _blurredRT.Release();
                Destroy(_blurredRT);
                _blurredRT = null;
            }
        }
    }
}
