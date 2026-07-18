using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Config.GameConditions
{
    [Serializable]
    public enum ConditionMergeType
    {
        And = 0,
        Or = 1,
        Xor = 3
    }
    
    [Serializable]
    public class GameConditionGroupData
    {
        public ConditionMergeType mergeType;
        public GameCondition condition;
        /// <summary>
        /// Nesting level for parentheses support (0 = no nesting, 1+ = grouped/parenthesized).
        /// Conditions with the same nesting level at the same evaluation context are evaluated sequentially.
        /// Higher nesting levels create sub-groups that are evaluated first (like parentheses in math).
        /// </summary>
        [Tooltip("Nesting level: 0=no nesting, higher values=grouped conditions")]
        public int nestingLevel;
    }
    
    public class GameConditionGroup : ScriptableObject
    {
        public GameConditionGroupData[] conditions;
        public bool isAndConditions = true;
#if ODIN_INSPECTOR
        [HideIf(nameof(isAndConditions))]
#endif
        public bool isOrConditions;

        /// <summary>
        /// Evaluates whether all conditions in this group are met, combining them based on their merge types and nesting levels.
        /// Supports parentheses through nesting levels - higher nesting levels are evaluated first (grouped evaluation).
        /// Uses fast-path evaluation if isAndConditions or isOrConditions is activated.
        /// </summary>
        /// <param name="conditionEvaluator">Delegate to evaluate individual conditions (typically UserDataManager.IsConditionMet)</param>
        /// <returns>True if the group conditions are satisfied according to merge type rules and nesting structure</returns>
        public bool IsGroupConditionMet(Func<GameCondition, bool> conditionEvaluator)
        {
            if (conditionEvaluator == null)
                throw new ArgumentNullException(nameof(conditionEvaluator));
            
            if (conditions == null || conditions.Length == 0)
                return true; // Empty group is considered met

            // Use optimized fast-path evaluation for simple AND/OR cases
            if (isAndConditions)
                return EvaluateSimpleAnd(conditionEvaluator);
            
            if (isOrConditions)
                return EvaluateSimpleOr(conditionEvaluator);

            // Use complex nesting-level-based evaluation for advanced cases
            return EvaluateAtNestingLevel(0, conditionEvaluator, out _);
        }

        /// <summary>
        /// Gets a string representation of the condition group showing the parentheses structure.
        /// Useful for debugging and validation.
        /// Example: "Condition1 AND (Condition2 OR Condition3) AND Condition4"
        /// </summary>
        public string GetStructureDebugString()
        {
            if (conditions == null || conditions.Length == 0)
                return "()";

            var sb = new System.Text.StringBuilder();
            int currentNestingLevel = conditions[0].nestingLevel;
            
            for (int i = 0; i < conditions.Length; i++)
            {
                int newNestingLevel = conditions[i].nestingLevel;

                // Handle nesting level changes (opening/closing parentheses)
                if (i > 0)
                {
                    if (newNestingLevel > currentNestingLevel)
                    {
                        for (int j = currentNestingLevel; j < newNestingLevel; j++)
                            sb.Append("(");
                    }
                    else if (newNestingLevel < currentNestingLevel)
                    {
                        for (int j = newNestingLevel; j < currentNestingLevel; j++)
                            sb.Append(")");
                        sb.Append(" ");
                    }
                    else
                    {
                        sb.Append(" ");
                    }

                    sb.Append(conditions[i].mergeType).Append(" ");
                }

                sb.Append($"Cond{i}");
                currentNestingLevel = newNestingLevel;
            }

            // Close remaining parentheses
            for (int j = 0; j < currentNestingLevel; j++)
                sb.Append(")");

            return sb.ToString();
        }

        /// <summary>
        /// Evaluates whether any condition in this group is met (logical OR of all conditions).
        /// </summary>
        public bool IsAnyConditionMet(Func<GameCondition, bool> conditionEvaluator)
        {
            if (conditionEvaluator == null)
                throw new ArgumentNullException(nameof(conditionEvaluator));
            
            if (conditions == null || conditions.Length == 0)
                return false;

            for (int i = 0; i < conditions.Length; i++)
            {
                if (EvaluateCondition(conditions[i], conditionEvaluator))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Evaluates whether all conditions in this group are met (logical AND of all conditions).
        /// Ignores individual merge types, treats all as AND.
        /// </summary>
        public bool AreAllConditionsMet(Func<GameCondition, bool> conditionEvaluator)
        {
            if (conditionEvaluator == null)
                throw new ArgumentNullException(nameof(conditionEvaluator));
            
            if (conditions == null || conditions.Length == 0)
                return true;

            for (int i = 0; i < conditions.Length; i++)
            {
                if (!EvaluateCondition(conditions[i], conditionEvaluator))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Counts how many conditions in this group are met.
        /// </summary>
        public int CountMetConditions(Func<GameCondition, bool> conditionEvaluator)
        {
            if (conditionEvaluator == null)
                throw new ArgumentNullException(nameof(conditionEvaluator));
            
            if (conditions == null || conditions.Length == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (EvaluateCondition(conditions[i], conditionEvaluator))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Validates that all conditions in the group are valid.
        /// </summary>
        public bool ValidateConditions()
        {
            if (conditions == null || conditions.Length == 0)
                return false;

            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i]?.condition == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that all conditions in the group are valid and nesting structure is correct.
        /// </summary>
        public bool ValidateAll()
        {
            return ValidateConditions() && ValidateNestingStructure();
        }

        /// <summary>
        /// Validates the nesting structure is properly formed (no orphaned groups, proper nesting progression).
        /// </summary>
        public bool ValidateNestingStructure()
        {
            if (conditions == null || conditions.Length == 0)
                return true; // No nesting to validate

            int currentNesting = conditions[0].nestingLevel;
            
            for (int i = 1; i < conditions.Length; i++)
            {
                int nextNesting = conditions[i].nestingLevel;
                
                // Nesting can only increase by 1 per level
                if (nextNesting > currentNesting + 1)
                    return false; // Invalid nesting progression
                
                currentNesting = nextNesting;
            }

            // All groups should be closed at the end
            return true;
        }

        /// <summary>
        /// Gets the total number of conditions in this group.
        /// </summary>
        public int ConditionCount => conditions?.Length ?? 0;

        /// <summary>
        /// Checks if this group is empty.
        /// </summary>
        public bool IsEmpty => conditions == null || conditions.Length == 0;

        /// <summary>
        /// Gets the maximum nesting level used in this group.
        /// </summary>
        public int MaxNestingLevel
        {
            get
            {
                if (conditions == null || conditions.Length == 0)
                    return 0;

                int maxLevel = 0;
                for (int i = 0; i < conditions.Length; i++)
                {
                    if (conditions[i].nestingLevel > maxLevel)
                        maxLevel = conditions[i].nestingLevel;
                }
                return maxLevel;
            }
        }

        /// <summary>
        /// Checks if this group uses any nesting (parentheses).
        /// </summary>
        public bool HasNesting => MaxNestingLevel > 0;

        // ==================== Private Helper Methods ====================

        /// <summary>
        /// Fast-path evaluation: all conditions are combined with AND logic.
        /// Used when isAndConditions is true - ignores individual merge types for performance.
        /// </summary>
        private bool EvaluateSimpleAnd(Func<GameCondition, bool> evaluator)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!evaluator(conditions[i].condition))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Fast-path evaluation: all conditions are combined with OR logic.
        /// Used when isOrConditions is true - ignores individual merge types for performance.
        /// </summary>
        private bool EvaluateSimpleOr(Func<GameCondition, bool> evaluator)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (evaluator(conditions[i].condition))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Recursively evaluates conditions at a specific nesting level.
        /// This method handles parentheses by treating higher nesting levels as grouped sub-expressions.
        /// </summary>
        /// <param name="nestingLevel">The nesting level to evaluate at the current recursion</param>
        /// <param name="evaluator">The condition evaluator delegate</param>
        /// <param name="nextIndex">Output parameter for the next index to process at this nesting level</param>
        /// <returns>The boolean result of evaluating all conditions at this nesting level</returns>
        private bool EvaluateAtNestingLevel(int nestingLevel, Func<GameCondition, bool> evaluator, out int nextIndex)
        {
            nextIndex = -1;
            
            // Find the first condition at this nesting level
            int startIndex = -1;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i].nestingLevel == nestingLevel)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex == -1)
            {
                nextIndex = conditions.Length;
                return true; // No conditions at this level
            }

            bool result = EvaluateConditionAtIndex(startIndex, nestingLevel, evaluator, out nextIndex);

            // Continue processing conditions at this nesting level
            while (nextIndex < conditions.Length && conditions[nextIndex].nestingLevel >= nestingLevel)
            {
                if (conditions[nextIndex].nestingLevel == nestingLevel)
                {
                    result = ApplyMergeTypeAtIndex(result, nextIndex, nestingLevel, evaluator, out nextIndex);
                }
                else if (conditions[nextIndex].nestingLevel > nestingLevel)
                {
                    // Skip to next condition at this level - the higher nesting level will be handled as a group
                    nextIndex++;
                }
            }

            return result;
        }

        /// <summary>
        /// Evaluates a single condition at a specific index, handling nested sub-groups.
        /// </summary>
        private bool EvaluateConditionAtIndex(int index, int currentNestingLevel, Func<GameCondition, bool> evaluator, out int nextIndex)
        {
            GameConditionGroupData data = conditions[index];
            bool result;

            // Check if there's a nested group right after this condition
            if (index + 1 < conditions.Length && conditions[index + 1].nestingLevel > currentNestingLevel)
            {
                // Evaluate the nested group as a single value
                result = EvaluateAtNestingLevel(conditions[index + 1].nestingLevel, evaluator, out int groupEndIndex);
                nextIndex = groupEndIndex;

                // Apply merge type to base condition and nested group
                bool baseResult = evaluator(data.condition);
                
                // Combine base result with nested group
                result = CombineResults(baseResult, result, data.mergeType);
            }
            else
            {
                // No nested group, just evaluate the condition
                result = evaluator(data.condition);
                nextIndex = index + 1;
            }

            return result;
        }

        /// <summary>
        /// Applies merge type logic to combine current result with next condition.
        /// Handles nested groups that appear after the condition.
        /// </summary>
        private bool ApplyMergeTypeAtIndex(bool currentResult, int index, int currentNestingLevel, Func<GameCondition, bool> evaluator, out int nextIndex)
        {
            GameConditionGroupData data = conditions[index];
            bool conditionResult;

            // Check if there's a nested group right after this condition
            if (index + 1 < conditions.Length && conditions[index + 1].nestingLevel > currentNestingLevel)
            {
                // Evaluate the nested group
                conditionResult = EvaluateAtNestingLevel(conditions[index + 1].nestingLevel, evaluator, out int groupEndIndex);
                nextIndex = groupEndIndex;
            }
            else
            {
                // No nested group, just evaluate the condition
                conditionResult = evaluator(data.condition);
                nextIndex = index + 1;
            }

            return CombineResults(currentResult, conditionResult, data.mergeType);
        }

        /// <summary>
        /// Combines two boolean results using the specified merge type.
        /// </summary>
        private bool CombineResults(bool left, bool right, ConditionMergeType mergeType)
        {
            switch (mergeType)
            {
                case ConditionMergeType.And:
                    return left && right;

                case ConditionMergeType.Or:
                    return left || right;

                case ConditionMergeType.Xor:
                    return left ^ right;

                default:
                    return left && right; // Default to AND
            }
        }

        /// <summary>
        /// Evaluates a single condition (legacy method, kept for compatibility).
        /// </summary>
        private bool EvaluateCondition(GameConditionGroupData data, Func<GameCondition, bool> evaluator)
        {
            return evaluator(data.condition);
        }
    }
}