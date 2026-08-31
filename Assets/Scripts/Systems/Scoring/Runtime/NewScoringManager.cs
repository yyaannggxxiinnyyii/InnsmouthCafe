using System;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.Customer;
using InnsmouthCafe.Decoration;

namespace InnsmouthCafe.Scoring
{
    /// <summary>
    /// 新评分管理器（长期经营版）。
    /// 评分规则：(咖啡品质 × 剩余耐心 × 美观度影响) → 11档星级。
    /// 使用 Singleton 自动创建实例，无需手动添加到场景。
    /// </summary>
    public class NewScoringManager : Singleton<NewScoringManager>
    {
        /// <summary>
        /// 评分完成事件。
        /// 参数：评分结果。
        /// </summary>
        public event Action<OrderScoringResult> OnScoringComplete;

        /// <summary>
        /// 计算订单评分。
        /// </summary>
        /// <param name="order">订单配置。</param>
        /// <param name="coffee">咖啡成品数据。</param>
        /// <param name="remainingPatienceRatio">剩余耐心比例（0-1）。</param>
        /// <param name="currentBeautyScore">当前美观度。</param>
        /// <returns>评分结果。</returns>
        public OrderScoringResult CalculateScore(
            OrderSO order,
            CoffeeData coffee,
            float remainingPatienceRatio,
            int currentBeautyScore = 0)
        {
            if (order == null || coffee == null)
            {
                Debug.LogError("[NewScoring] 订单或咖啡数据为空");
                return null;
            }

            // 如果没有传入美观度，从 DecorationManager 读取
            if (currentBeautyScore <= 0 && DecorationManager.Instance != null)
            {
                currentBeautyScore = DecorationManager.Instance.CurrentBeautyScore;
            }

            var result = new OrderScoringResult();

            // 1. 计算咖啡品质评分（0-100%）
            result.coffeeQualityScore = CalculateCoffeeQuality(order, coffee);

            // 2. 应用耐心值影响
            result.patienceMultiplier = GetPatienceMultiplier(remainingPatienceRatio);
            result.baseScore = result.coffeeQualityScore * result.patienceMultiplier;

            // 3. 应用美观度评分上限
            result.beautyScoreCap = GetBeautyScoreCap(currentBeautyScore);
            result.cappedScore = Mathf.Min(result.baseScore, result.beautyScoreCap);

            // 4. 映射到11档星级
            result.starRating = MapScoreToStars(result.cappedScore);

            // 5. 判定是否超时
            result.isTimeout = remainingPatienceRatio <= 0;
            if (result.isTimeout)
            {
                result.starRating = Mathf.Min(result.starRating, 2.5f); // 超时最高2.5星
            }

            // 6. 生成反馈文本
            result.feedbackText = GenerateFeedbackText(result);

            OnScoringComplete?.Invoke(result);

            Debug.Log($"[NewScoring] 评分完成：品质{result.coffeeQualityScore:F1}% × 耐心{result.patienceMultiplier:F2} = {result.baseScore:F1}，上限{result.beautyScoreCap:F1}，最终{result.starRating:F1}星");

            return result;
        }

        /// <summary>
        /// 计算咖啡品质评分（0-100%）。
        /// </summary>
        private float CalculateCoffeeQuality(OrderSO order, CoffeeData coffee)
        {
            // TODO: 复用旧 ScoringManager 的逻辑
            // 这里暂时返回固定值，等连接制作系统时再实现
            return 85f;
        }

        /// <summary>
        /// 根据剩余耐心获取评分倍率。
        /// </summary>
        /// <param name="remainingPatienceRatio">剩余耐心比例（0-1）。</param>
        /// <returns>评分倍率。</returns>
        private float GetPatienceMultiplier(float remainingPatienceRatio)
        {
            if (remainingPatienceRatio > 0.8f)
                return 1.2f; // 耐心 >80% → ×1.2

            if (remainingPatienceRatio > 0.6f)
                return 1.1f; // 耐心 60-80% → ×1.1

            if (remainingPatienceRatio > 0.4f)
                return 1.0f; // 耐心 40-60% → ×1.0

            if (remainingPatienceRatio > 0.2f)
                return 0.9f; // 耐心 20-40% → ×0.9

            return 0.8f; // 耐心 <20% → ×0.8
        }

        /// <summary>
        /// 根据美观度获取评分上限。
        /// </summary>
        /// <param name="beautyScore">当前美观度。</param>
        /// <returns>评分上限（百分制）。</returns>
        private float GetBeautyScoreCap(int beautyScore)
        {
            if (beautyScore < 300)
                return 80f; // 美观度 <300 → 上限4.0星（80分）

            if (beautyScore < 500)
                return 90f; // 美观度 300-500 → 上限4.5星（90分）

            return 100f; // 美观度 ≥500 → 上限5.0星（100分）
        }

        /// <summary>
        /// 将百分制评分映射到11档星级（0, 0.5, 1.0, ..., 5.0）。
        /// </summary>
        /// <param name="score">百分制评分（0-100）。</param>
        /// <returns>星级（0-5，0.5为单位）。</returns>
        private float MapScoreToStars(float score)
        {
            // 百分制转5星制：score / 20
            float stars = score / 20f;

            // 四舍五入到最近的0.5星
            stars = Mathf.Round(stars * 2f) / 2f;

            // 限制在0-5星
            return Mathf.Clamp(stars, 0f, 5f);
        }

        /// <summary>
        /// 生成反馈文本。
        /// </summary>
        /// <param name="result">评分结果。</param>
        /// <returns>反馈文本。</returns>
        private string GenerateFeedbackText(OrderScoringResult result)
        {
            if (result.starRating >= 4.5f)
                return "完美！这正是我想要的！";

            if (result.starRating >= 4.0f)
                return "非常不错，很满意！";

            if (result.starRating >= 3.0f)
                return "还不错，谢谢。";

            if (result.starRating >= 2.0f)
                return "一般般吧...";

            if (result.isTimeout)
                return "等太久了，不太满意。";

            return "这不是我要的...";
        }
    }

    /// <summary>
    /// 订单评分结果。
    /// 包含咖啡品质、耐心影响、美观度上限、最终星级等详细信息。
    /// </summary>
    [System.Serializable]
    public class OrderScoringResult
    {
        [Tooltip("咖啡品质评分（0-100%）")]
        public float coffeeQualityScore;

        [Tooltip("耐心值影响倍率")]
        public float patienceMultiplier;

        [Tooltip("基础评分（品质 × 耐心）")]
        public float baseScore;

        [Tooltip("美观度评分上限")]
        public float beautyScoreCap;

        [Tooltip("应用上限后的评分")]
        public float cappedScore;

        [Tooltip("最终星级（0-5，0.5为单位）")]
        public float starRating;

        [Tooltip("是否超时")]
        public bool isTimeout;

        [Tooltip("反馈文本")]
        public string feedbackText;
    }
}
