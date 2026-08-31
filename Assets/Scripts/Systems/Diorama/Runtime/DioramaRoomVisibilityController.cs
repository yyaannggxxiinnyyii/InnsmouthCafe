using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.Diorama
{
    /// <summary>
    /// 控制前台与后厨两个箱庭视觉组的显示状态，避免相机看到相邻房间。
    /// </summary>
    public class DioramaRoomVisibilityController : MonoBehaviour
    {
        [Header("视觉组")]
        [SerializeField]
        [Tooltip("前台房间的视觉根物体")]
        private GameObject _frontDeskRoom;

        [SerializeField]
        [Tooltip("后厨房间的视觉根物体，可填入一个 KitchenRoom，也可填入初加工和精加工两个房间")]
        private GameObject[] _kitchenRooms;

        [Header("引用")]
        [SerializeField]
        [Tooltip("箱庭相机控制器；留空时自动查找")]
        private DioramaCameraViewController _cameraViewController;

        private void Start()
        {
            ResolveReferences();

            if (_cameraViewController == null)
            {
                Debug.LogWarning("[DioramaRoom] 未找到相机视角控制器，无法同步房间显示状态", this);
                return;
            }

            _cameraViewController.OnViewTransitionStarted += HandleTransitionStarted;
            _cameraViewController.OnViewTransitionCompleted += HandleTransitionCompleted;

            ViewSwitchManager viewSwitchManager = ViewSwitchManager.Instance;
            if (viewSwitchManager != null)
            {
                ApplySteadyState(viewSwitchManager.CurrentViewType);
            }
        }

        private void OnDestroy()
        {
            if (_cameraViewController == null)
            {
                return;
            }

            _cameraViewController.OnViewTransitionStarted -= HandleTransitionStarted;
            _cameraViewController.OnViewTransitionCompleted -= HandleTransitionCompleted;
        }

        /// <summary>
        /// 在 Inspector 留空时查找当前物体上的相机视角控制器。
        /// </summary>
        private void ResolveReferences()
        {
            if (_cameraViewController == null)
            {
                _cameraViewController = GetComponent<DioramaCameraViewController>();
            }

            if (_cameraViewController == null)
            {
                _cameraViewController = FindObjectOfType<DioramaCameraViewController>();
            }
        }

        /// <summary>
        /// 过场开始时提前启用目标视觉组，让目标房间在相机移动过程中可被渲染。
        /// </summary>
        private void HandleTransitionStarted(GameViewType targetView)
        {
            if (targetView == GameViewType.Bar)
            {
                SetActive(_frontDeskRoom, true);
                return;
            }

            SetKitchenRoomsActive(true);
        }

        /// <summary>
        /// 相机到达目标后关闭另一视觉组，保持稳定视角下只有目标空间可见。
        /// </summary>
        private void HandleTransitionCompleted(GameViewType targetView)
        {
            ApplySteadyState(targetView);
        }

        /// <summary>
        /// 根据当前经营界面设置两个视觉组的稳定显示状态。
        /// </summary>
        private void ApplySteadyState(GameViewType viewType)
        {
            bool isFrontDesk = viewType == GameViewType.Bar;
            SetActive(_frontDeskRoom, isFrontDesk);
            SetKitchenRoomsActive(!isFrontDesk);
        }

        /// <summary>
        /// 设置后厨所有视觉根物体的激活状态。
        /// </summary>
        private void SetKitchenRoomsActive(bool isActive)
        {
            if (_kitchenRooms == null)
            {
                return;
            }

            foreach (GameObject kitchenRoom in _kitchenRooms)
            {
                SetActive(kitchenRoom, isActive);
            }
        }

        /// <summary>
        /// 安全设置指定视觉根物体的激活状态。
        /// </summary>
        private static void SetActive(GameObject room, bool isActive)
        {
            if (room == null || room.activeSelf == isActive)
            {
                return;
            }

            room.SetActive(isActive);
        }
    }
}
