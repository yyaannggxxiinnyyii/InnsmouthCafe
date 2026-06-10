using System;
using UnityEngine;
using InnsmouthCafe.Data;

/// <summary>
/// 咖啡评分系统管理器
/// 负责计算咖啡的各维度评分和最终档位
/// </summary>
public class ScoringManager : Singleton<ScoringManager>
{
        [Header("评分权重（各项相加应为 1.0）")]
        [SerializeField] [Range(0, 1)]
        [Tooltip("咖啡液匹配权重：豆种+研磨度+萃取量是否符合订单要求")]
        private float _coffeeMatchWeight = 0.25f;

        [SerializeField] [Range(0, 1)]
        [Tooltip("辅助液匹配权重：各辅助液种类和用量是否符合订单要求")]
        private float _liquidMatchWeight = 0.25f;

        [SerializeField] [Range(0, 1)]
        [Tooltip("小料匹配权重：指定小料是否全部添加")]
        private float _toppingMatchWeight = 0.1f;

        [SerializeField] [Range(0, 1)]
        [Tooltip("总量匹配权重：咖啡液+辅助液总量是否接近订单目标量")]
        private float _volumeMatchWeight = 0.25f;

        [SerializeField] [Range(0, 1)]
        [Tooltip("杯子适配权重：液体填充比例是否在最佳区间内（太少或太满都会扣分）")]
        private float _cupAdaptationWeight = 0.15f;

        [Header("容错范围（普通模式，单位 %）")]
        [SerializeField] [Range(0, 50)]
        [Tooltip("普通模式下辅助液/总量的允许偏差百分比，在此范围内视为满分（默认10%）")]
        private float _normalModeErrorMargin = 10f;

        [Header("杯子适配配置")]
        [SerializeField] [Range(0.4f, 0.9f)]
        [Tooltip("最佳填充比例下限：液体量低于此比例时开始扣分（默认0.6，即杯容量的60%）")]
        private float _optimalFillRatioLow = 0.6f;

        [SerializeField] [Range(0.6f, 1.0f)]
        [Tooltip("最佳填充比例上限：液体量高于此比例时开始扣分（默认0.95，即杯容量的95%）")]
        private float _optimalFillRatioHigh = 0.95f;

        [Header("溢出惩罚")]
        [SerializeField]
        [Tooltip("每溢出25ml扣除的理智值（默认0.2）；溢出还会使最终得分×0.9")]
        private float _sanityLossPerOverflow25ml = 0.2f;

        [Header("评分档位分界线（分）")]
        [SerializeField] [Range(0, 100)]
        [Tooltip("低于此分为「糟糕」档（Terrible）→ 顾客不满意；达到此分及以上为「合格」（Acceptable）→ 顾客一般；90分及以上为「完美」（Perfect）→ 顾客满意+收集物")]
        private float _acceptableThreshold = 50f;

        public event Action<CoffeeScoringData> OnScoringComplete;

        public CoffeeScoringData CalculateScore(OrderSO order, CoffeeData coffee, GameMode gameMode = GameMode.Normal)
        {
            if (order == null || coffee == null)
            {
                Debug.LogError("[Scoring] 订单或咖啡数据为空");
                return null;
            }

            CoffeeScoringData score = new CoffeeScoringData();
            score.hasOverflow = coffee.isOverflowed;

            if (coffee.isOverflowed)
            {
                float totalVolume = coffee.GetTotalCoffeeVolume() + coffee.GetTotalLiquidVolume();
                if (coffee.selectedCup != null)
                {
                    float overflowVolume = Mathf.Max(0, totalVolume - coffee.selectedCup.capacity);
                    score.overflowSanityLoss = (overflowVolume / 25f) * _sanityLossPerOverflow25ml;
                    Debug.Log($"[Scoring] 溢出 {overflowVolume:F1}ml，理智损失 {score.overflowSanityLoss:F1}");
                }
            }

            score.coffeeMatchScore = CalculateCoffeeMatchScore(order, coffee);
            score.liquidMatchScore = CalculateLiquidMatchScore(order, coffee, gameMode);
            score.toppingMatchScore = CalculateToppingMatchScore(order, coffee);
            score.volumeMatchScore = CalculateVolumeMatchScore(order, coffee, gameMode);
            score.cupAdaptationScore = CalculateCupAdaptationScore(coffee);

            // 各子评分为0-100，先除以100归一化到0-1再加权，最终乘100得到百分制
            score.finalScore = Mathf.Clamp(
                score.coffeeMatchScore * _coffeeMatchWeight +
                score.liquidMatchScore * _liquidMatchWeight +
                score.toppingMatchScore * _toppingMatchWeight +
                score.volumeMatchScore * _volumeMatchWeight +
                score.cupAdaptationScore * _cupAdaptationWeight,
                0f, 100f
            );

            if (coffee.isOverflowed)
            {
                score.finalScore *= 0.9f;
            }

            score.qualityLevel = MapScoreToQuality(score.finalScore);
            score.feedbackLevel = MapQualityToFeedback(score.qualityLevel);
            score.scoringDetail = GenerateScoringDetail(order, coffee, score);

            Debug.Log(score.ToString());
            OnScoringComplete?.Invoke(score);

            return score;
        }

        private float CalculateCoffeeMatchScore(OrderSO order, CoffeeData coffee)
        {
            var orderData = order.ToData();
            if (orderData.coffeeRequirements.Count == 0 || coffee.coffeeSegments.Count == 0)
                return 50f;

            float totalErrorRatio = 0f;
            int matchCount = 0;

            foreach (var requirement in orderData.coffeeRequirements)
            {
                var matchingSegment = coffee.coffeeSegments.Find(seg =>
                    seg.bean == requirement.bean && seg.grindType == requirement.grindType);

                if (matchingSegment != null)
                {
                    float errorRatio = Mathf.Abs(matchingSegment.extractedVolume - requirement.targetVolume) / requirement.targetVolume;
                    totalErrorRatio += errorRatio;
                    matchCount++;
                }
                else
                {
                    totalErrorRatio += 1.0f;
                    matchCount++;
                }
            }

            float averageError = matchCount > 0 ? totalErrorRatio / matchCount : 1.0f;
            float score = Mathf.Clamp01(1.0f - averageError) * 100f;
            Debug.Log($"[Scoring] 咖啡液匹配得分: {score:F1} (平均偏差: {averageError * 100:F1}%)");
            return score;
        }

        private float CalculateLiquidMatchScore(OrderSO order, CoffeeData coffee, GameMode gameMode)
        {
            var orderData = order.ToData();
            if (orderData.liquidRequirements.Count == 0)
                return coffee.liquidSegments.Count == 0 ? 100f : 80f;

            if (coffee.liquidSegments.Count == 0)
                return 20f;

            float errorMargin = GetErrorMargin(gameMode);
            float totalErrorRatio = 0f;
            int matchCount = 0;

            foreach (var requirement in orderData.liquidRequirements)
            {
                float actualVolume = 0f;
                foreach (var segment in coffee.liquidSegments)
                {
                    if (segment.liquid == requirement.liquid)
                        actualVolume += segment.amountMl;
                }

                float errorRatio = Mathf.Abs(actualVolume - requirement.targetVolume) / Mathf.Max(1, requirement.targetVolume);
                totalErrorRatio += (errorRatio <= errorMargin / 100f) ? 0 : errorRatio;
                matchCount++;
            }

            float averageError = matchCount > 0 ? totalErrorRatio / matchCount : 0f;
            float score = Mathf.Clamp01(1.0f - averageError) * 100f;
            Debug.Log($"[Scoring] 辅助液匹配得分: {score:F1}");
            return score;
        }

        private float CalculateToppingMatchScore(OrderSO order, CoffeeData coffee)
        {
            var orderData = order.ToData();
            if (orderData.toppingRequirement == null || orderData.toppingRequirement.requiredToppings == null || orderData.toppingRequirement.requiredToppings.Count == 0)
                return coffee.toppings.Count == 0 ? 100f : 90f;

            bool allRequiredToppingsPresent = true;
            foreach (var requiredTopping in orderData.toppingRequirement.requiredToppings)
            {
                if (!coffee.toppings.Exists(t => t.topping == requiredTopping))
                {
                    allRequiredToppingsPresent = false;
                    break;
                }
            }

            float score = 100f;
            if (!allRequiredToppingsPresent) score -= 30f;
            score = Mathf.Max(0, score);
            return score;
        }

        private float CalculateVolumeMatchScore(OrderSO order, CoffeeData coffee, GameMode gameMode)
        {
            var orderData = order.ToData();
            float targetVolume = orderData.targetTotalVolume;
            float actualVolume = coffee.currentTotalVolume;
            float errorMargin = GetErrorMargin(gameMode);
            float errorRatio = Mathf.Abs(actualVolume - targetVolume) / Mathf.Max(1, targetVolume);

            if (errorRatio <= errorMargin / 100f)
                return 100f;

            float score = Mathf.Clamp01(1.0f - errorRatio) * 100f;
            return score;
        }

        private float CalculateCupAdaptationScore(CoffeeData coffee)
        {
            if (coffee.selectedCup == null)
                return 0f;

            float fillRatio = Mathf.Clamp01(coffee.currentTotalVolume / coffee.selectedCup.capacity);
            float optimalCenter = (_optimalFillRatioLow + _optimalFillRatioHigh) / 2f;
            float distanceToOptimal = Mathf.Abs(fillRatio - optimalCenter);
            float maxDistance = Mathf.Max(optimalCenter - _optimalFillRatioLow, _optimalFillRatioHigh - optimalCenter);
            float score = Mathf.Clamp01(1.0f - (distanceToOptimal / maxDistance)) * 100f;
            return score;
        }

        private CoffeeQuality MapScoreToQuality(float score)
        {
            if (score >= 90f) return CoffeeQuality.Perfect;
            if (score >= _acceptableThreshold) return CoffeeQuality.Acceptable;
            return CoffeeQuality.Terrible;
        }

        private int MapQualityToFeedback(CoffeeQuality quality)
        {
            return quality switch
            {
                CoffeeQuality.Perfect    => 2,
                CoffeeQuality.Acceptable => 1,
                CoffeeQuality.Terrible   => 0,
                _                        => 0
            };
        }

        private float GetErrorMargin(GameMode gameMode)
        {
            return gameMode switch
            {
                GameMode.Tutorial => 30f,
                GameMode.Normal   => _normalModeErrorMargin,
                _                 => _normalModeErrorMargin
            };
        }

        private string GenerateScoringDetail(OrderSO order, CoffeeData coffee, CoffeeScoringData score)
        {
            var orderData = order.ToData();
            string detail = $"订单目标: {orderData.targetTotalVolume:F0}ml\n实际总量: {coffee.currentTotalVolume:F1}ml\n杯子容量: {(coffee.selectedCup?.capacity ?? 0):F0}ml\n";
            if (score.hasOverflow)
                detail += $"⚠️ 发生溢出\n";
            if (score.hasMissingComponents)
                detail += "⚠️ 缺少必要成分\n";
            return detail;
        }
    }
