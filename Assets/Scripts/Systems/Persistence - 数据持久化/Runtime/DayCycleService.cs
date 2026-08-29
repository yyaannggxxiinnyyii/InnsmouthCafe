using System;
using InnsmouthCafe.Persistence;
using UnityEngine;

namespace InnsmouthCafe.GameFlow
{
    /// <summary>
    /// 管理当前存档中的天数与一天阶段切换。
    /// 不负责地图、章节、库存或顾客等具体玩法系统。
    /// </summary>
    [DisallowMultipleComponent]
    public class DayCycleService : MonoBehaviour
    {
        private static DayCycleService _instance;

        /// <summary>
        /// 获取一天阶段服务单例。
        /// </summary>
        public static DayCycleService Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject serviceObject = new GameObject(nameof(DayCycleService));
                    _instance = serviceObject.AddComponent<DayCycleService>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 当前存档的天数；没有加载存档时返回 0。
        /// </summary>
        public int CurrentDay => SaveSlotService.Instance.CurrentSave?.currentDay ?? 0;

        /// <summary>
        /// 当前存档的一天阶段；没有加载存档时返回探索阶段作为默认值。
        /// </summary>
        public DayPhase CurrentPhase => SaveSlotService.Instance.CurrentSave?.currentPhase
            ?? DayPhase.Exploration;

        /// <summary>
        /// 阶段切换成功后触发，参数为新的阶段。
        /// </summary>
        public event Action<DayPhase> OnPhaseChanged;

        /// <summary>
        /// 完成日结算并进入下一天后触发，参数为新的一天。
        /// </summary>
        public event Action<int> OnDayStarted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 初始化当前存档的阶段字段，并将缺省天数修正为第 1 天。
        /// </summary>
        public bool InitializeCurrentSave()
        {
            GameSaveData saveData = SaveSlotService.Instance.CurrentSave;
            if (saveData == null)
            {
                return false;
            }

            if (saveData.currentDay < 1)
            {
                saveData.currentDay = 1;
                saveData.currentPhase = DayPhase.Exploration;
                return SaveSlotService.Instance.SaveCurrent();
            }

            return true;
        }

        /// <summary>
        /// 按探索、备料、营业、结算的固定顺序推进一个阶段。
        /// 结算阶段推进成功后会进入下一天的探索阶段。
        /// </summary>
        public bool TryAdvancePhase()
        {
            GameSaveData saveData = SaveSlotService.Instance.CurrentSave;
            if (saveData == null)
            {
                Debug.LogWarning("[DayCycle] 当前没有已加载的存档，无法推进阶段。");
                return false;
            }

            DayPhase previousPhase = saveData.currentPhase;
            int previousDay = saveData.currentDay;

            if (saveData.currentPhase == DayPhase.Settlement)
            {
                saveData.currentDay = Mathf.Max(1, saveData.currentDay + 1);
                saveData.currentPhase = DayPhase.Exploration;
            }
            else
            {
                saveData.currentPhase = (DayPhase)((int)saveData.currentPhase + 1);
            }

            if (!SaveSlotService.Instance.SaveCurrent())
            {
                saveData.currentDay = previousDay;
                saveData.currentPhase = previousPhase;
                return false;
            }

            OnPhaseChanged?.Invoke(saveData.currentPhase);
            if (previousPhase == DayPhase.Settlement)
            {
                OnDayStarted?.Invoke(saveData.currentDay);
            }

            return true;
        }
    }
}
