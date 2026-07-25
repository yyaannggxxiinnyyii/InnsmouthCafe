using System;
using UnityEngine;
using InnsmouthCafe.Data;

/// <summary>
/// 咖啡评分系统管理器
/// 负责计算咖啡的各维度评分和最终档位
/// </summary>
public class ScoringManager : Singleton<ScoringManager>
{
        public event Action<CoffeeScoringData> OnScoringComplete;

        public CoffeeScoringData CalculateScore(
            OrderSO order,
            CoffeeData coffee,
            GameModeConfigSO gameModeConfig = null)
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
                    CoffeeCraftBalanceSettings craftBalance = GameplayBalanceManager.Instance.Config.CoffeeCraft;
                    score.overflowSanityLoss = overflowVolume
                        / craftBalance.overflowPenaltyInterval
                        * craftBalance.overflowSanityPenalty;
                    Debug.Log($"[Scoring] 溢出 {overflowVolume:F1}ml，理智损失 {score.overflowSanityLoss:F1}");
                }
            }

            score.coffeeMatchScore = CalculateCoffeeMatchScore(order, coffee);
            score.liquidMatchScore = CalculateLiquidMatchScore(order, coffee, gameModeConfig);
            score.toppingMatchScore = CalculateToppingMatchScore(order, coffee);
            score.volumeMatchScore = CalculateVolumeMatchScore(order, coffee, gameModeConfig);
            score.cupAdaptationScore = CalculateCupAdaptationScore(coffee);

            // 各子评分为0-100，先除以100归一化到0-1再加权，最终乘100得到百分制
            ScoringBalanceSettings balance = GameplayBalanceManager.Instance.Config.Scoring;
            score.finalScore = Mathf.Clamp(
                score.coffeeMatchScore * balance.coffeeMatchWeight +
                score.liquidMatchScore * balance.liquidMatchWeight +
                score.toppingMatchScore * balance.toppingMatchWeight +
                score.volumeMatchScore * balance.volumeMatchWeight +
                score.cupAdaptationScore * balance.cupAdaptationWeight,
                0f, 100f
            );

            if (coffee.isOverflowed)
            {
                score.finalScore *= balance.overflowScoreMultiplier;
            }

            score.qualityLevel = MapScoreToQuality(score.finalScore, gameModeConfig);
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

        private float CalculateLiquidMatchScore(
            OrderSO order,
            CoffeeData coffee,
            GameModeConfigSO gameModeConfig)
        {
            var orderData = order.ToData();
            if (orderData.liquidRequirements.Count == 0)
                return coffee.liquidSegments.Count == 0 ? 100f : 80f;

            if (coffee.liquidSegments.Count == 0)
                return 20f;

            float errorMargin = GetErrorMargin(gameModeConfig);
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

        private float CalculateVolumeMatchScore(
            OrderSO order,
            CoffeeData coffee,
            GameModeConfigSO gameModeConfig)
        {
            var orderData = order.ToData();
            float targetVolume = orderData.targetTotalVolume;
            float actualVolume = coffee.currentTotalVolume;
            float errorMargin = GetErrorMargin(gameModeConfig);
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
            ScoringBalanceSettings balance = GameplayBalanceManager.Instance.Config.Scoring;
            float optimalCenter = (balance.optimalFillRatioLow + balance.optimalFillRatioHigh) / 2f;
            float distanceToOptimal = Mathf.Abs(fillRatio - optimalCenter);
            float maxDistance = Mathf.Max(
                optimalCenter - balance.optimalFillRatioLow,
                balance.optimalFillRatioHigh - optimalCenter);
            float score = Mathf.Clamp01(1.0f - (distanceToOptimal / maxDistance)) * 100f;
            return score;
        }

        private CoffeeQuality MapScoreToQuality(float score, GameModeConfigSO gameModeConfig)
        {
            float acceptableThreshold = gameModeConfig != null
                ? gameModeConfig.GetAcceptableScoreThreshold()
                : GameplayBalanceManager.Instance.Config.Scoring.acceptableScoreThreshold;
            float perfectThreshold = gameModeConfig != null
                ? gameModeConfig.GetPerfectScoreThreshold()
                : GameplayBalanceManager.Instance.Config.Scoring.perfectScoreThreshold;

            if (score >= perfectThreshold) return CoffeeQuality.Perfect;
            if (score >= acceptableThreshold) return CoffeeQuality.Acceptable;
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

        private float GetErrorMargin(GameModeConfigSO gameModeConfig)
        {
            return gameModeConfig != null
                ? gameModeConfig.GetScoringErrorMargin()
                : GameplayBalanceManager.Instance.Config.Scoring.defaultErrorMargin;
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
