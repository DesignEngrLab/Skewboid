using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdjustablePareto
{
    internal static class ParetoFunctions
    {
        static int numObjectives;
        static double mu;
        static double[] weights;
        static optimize[] optDirections;

        internal static List<ICandidate> FindParetoCandidates(List<ICandidate> candidates, double _mu,
            double[] _weights, optimize[] _optDirections = null)
        {
            List<ICandidate> paretoSet = new List<ICandidate>();
            numObjectives = candidates[0].objectives.GetLength(0);
            mu = _mu;
            if (_weights != null) weights = (double[])_weights.Clone();
            else weights = null;
            if (_optDirections == null)
            {
                optDirections = new optimize[numObjectives];
                for (int i = 0; i < numObjectives; i++)
                    optDirections[i] = optimize.minimize;
            }
            else optDirections = (optimize[])_optDirections.Clone();
            if (weights != null)
                foreach (var c in candidates)
                    UpdateParetoWithWeights(paretoSet, c);
            else
                foreach (var c in candidates)
                    UpdatePareto(paretoSet, c);

            return paretoSet;
        }

        private static void UpdateParetoWithWeights(List<ICandidate> paretoSet, ICandidate c)
        {
            for (int i = paretoSet.Count - 1; i >= 0; i--)
            {
                var pc = paretoSet[i];
                if (dominatesWithWeights(c, pc))
                    paretoSet.Remove(pc);
                else if (dominatesWithWeights(pc, c)) return;
            }
            paretoSet.Add(c);
        }

        /// <summary>
        /// Does c1 dominate c2?
        /// </summary>
        /// <param name="c1">the subject candidate, c1 (does this dominate...).</param>
        /// <param name="c2">the object candidate, c2 (is dominated by).</param>
        /// <returns></returns>
        private static Boolean dominatesWithWeights(ICandidate c1, ICandidate c2)
        {
            for (int i = 0; i < numObjectives; i++)
            {
                double c1Value = 0.0, c2Value = 0.0;
                for (int j = 0; j < numObjectives; j++)
                {
                    if (j == i)
                    {
                        c1Value += weights[j] * c1.objectives[j];
                        c2Value += weights[j] * c2.objectives[j];
                    }
                    else
                    {
                        c1Value += mu * weights[j] * c1.objectives[j];
                        c2Value += mu * weights[j] * c2.objectives[j];
                    }
                }
                if (((int)optDirections[i]) * c1Value < ((int)optDirections[i]) * c2Value)
                    return false;
            }
            return true;
        }


        private static void UpdatePareto(List<ICandidate> paretoSet, ICandidate c)
        {
            for (int i = paretoSet.Count - 1; i >= 0; i--)
            {
                var pc = paretoSet[i];
                if (dominates(c, pc))
                    paretoSet.Remove(pc);
                else if (dominates(pc, c)) return;
            }
            paretoSet.Add(c);
        }

        /// <summary>
        /// Does c1 dominate c2?
        /// </summary>
        /// <param name="c1">the subject candidate, c1 (does this dominate...).</param>
        /// <param name="c2">the object candidate, c2 (is dominated by).</param>
        /// <returns></returns>
        private static Boolean dominates(ICandidate c1, ICandidate c2)
        {
            for (int i = 0; i < numObjectives; i++)
            {
                double c1Value = (numObjectives - 1) * mu + 1;
                double c2Value = 0.0;
                for (int j = 0; j < numObjectives; j++)
                {
                    if (j == i) c2Value +=  c2.objectives[j] / c1.objectives[j];
                    else c2Value += mu * c2.objectives[j] / c1.objectives[j];
                }
                if (((int)optDirections[i]) * c1Value < ((int)optDirections[i]) * c2Value)
                    return false;
            }
            return true;
        }
    }

    ///<summary>
    /// Enumerator for Search functions that have generality
    /// to either minimize or maximize (e.g. PNPPS, stochasticChoose). */
    ///</summary>
    internal enum optimize
    {
        /// <summary>
        /// Minimize in the search - smaller is better.
        /// </summary>
        minimize = -1,
        /// <summary>
        /// Maximize in the search - bigger is better.
        /// </summary>
        maximize = 1
    };
}

