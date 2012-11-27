using System;
using System.Collections.Generic;

namespace SkewBoid
{
    internal static class ParetoFunctions
    {
        private static int numObjectives;
        private static double alpha;
        private static double[] weights;
        private static optimize[] optDirections;
        private const double tolerance = 0.0001; // tolerance

        internal static List<ICandidate> FindParetoCandidates(List<ICandidate> candidates, double _alpha,
                                                              double[] _weights, optimize[] _optDirections = null)
        {
            initializeWeightsAndDirections(candidates, _weights, _optDirections);
            alpha = _alpha;
            List<ICandidate> paretoSet = new List<ICandidate>();
            numObjectives = candidates[0].objectives.GetLength(0);
            alpha = _alpha;
            if (_weights != null) weights = (double[]) _weights.Clone();
            else weights = null;
            if (_optDirections == null)
            {
                optDirections = new optimize[numObjectives];
                for (int i = 0; i < numObjectives; i++)
                    optDirections[i] = optimize.minimize;
            }
            else optDirections = (optimize[]) _optDirections.Clone();
            if (weights != null)
                foreach (var c in candidates)
                    UpdateParetoWithWeights(paretoSet, c);
            else
                foreach (var c in candidates)
                    UpdatePareto(paretoSet, c);

            return paretoSet;
        }


        /// <summary>
        /// Finds the given number candidates by iterating (root-finding) on alpha.
        /// </summary>
        /// <param name="candidates">The candidates.</param>
        /// <param name="numKeep">The num keep.</param>
        /// <param name="alphaTarget">The alpha target.</param>
        /// <param name="_weights">The _weights.</param>
        /// <param name="_optDirections">The _opt directions.</param>
        /// <returns></returns>
        internal static List<ICandidate> FindGivenNumCandidates(List<ICandidate> candidates, int numKeep, out double alphaTarget,
                                                              double[] _weights, optimize[] _optDirections = null)
        {
            initializeWeightsAndDirections(candidates, _weights, _optDirections);
            double alphaLB, alphaUB;
            int numatLB, numatUB;
            var paretoSet = FindParetoCandidates(candidates, 0, weights);
            var numTarget = paretoSet.Count;
            if (numTarget == numKeep)
            {
                alphaTarget = 0;
                return paretoSet;
            }
            if (numTarget < numKeep)
            {
                /* not enough in the real pareto - need to relax */
                alphaLB = -1;
                numatLB = candidates.Count;
                alphaUB = 0;
                numatUB = numTarget;
                if (numatLB <= numKeep)
                {
                    alphaTarget = alphaLB;
                    return candidates;
                }
            }
            else
            {
                /* too manyin the real pareto - need to filter */
                alphaLB = 0;
                numatLB = numTarget;
                alphaUB = 1;
                paretoSet = FindParetoCandidates(candidates, alphaUB, weights);
                numatUB = paretoSet.Count;
                if (numatUB >= numKeep)
                {
                    alphaTarget = alphaUB;
                    return paretoSet;
                }
            }
            alphaTarget = double.NaN;
            /* debugger requires alphaTarget to be assigned, and it is worried that
                                   * the while loop will be passed over completely, hence need this line.
                                   * so, if that indeed happens, we throw an error. */
            int k = 0;
            while (numKeep != numTarget && alphaUB - alphaLB > tolerance)
            {
                k++;
                alphaTarget = alphaLB + (alphaUB - alphaLB)*(numatLB - numKeep)/(numatLB - numatUB);
                paretoSet = FindParetoCandidates(candidates, alphaTarget, weights);
                numTarget = paretoSet.Count;
                if (numTarget>numKeep)
                {
                    alphaLB = alphaTarget;
                    numatLB = numTarget;
                }
                else if (numTarget < numKeep)
                {
                    alphaUB = alphaTarget;
                    numatUB = numTarget;
                }
            }
            if (double.IsNaN(alphaTarget)) throw new Exception("Somehow the while loop was passed over.");
            return paretoSet;
        }

        private static void initializeWeightsAndDirections(List<ICandidate> candidates, double[] _weights, optimize[] _optDirections)
        {
            numObjectives = candidates[0].objectives.GetLength(0);
            if (_weights != null) weights = (double[])_weights.Clone();
            else weights = null;
            if (_optDirections == null)
            {
                optDirections = new optimize[numObjectives];
                for (int i = 0; i < numObjectives; i++)
                    optDirections[i] = optimize.minimize;
            }
            else optDirections = (optimize[])_optDirections.Clone();
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
                        c1Value += weights[j]*c1.objectives[j];
                        c2Value += weights[j]*c2.objectives[j];
                    }
                    else
                    {
                        c1Value += alpha*weights[j]*c1.objectives[j];
                        c2Value += alpha*weights[j]*c2.objectives[j];
                    }
                }
                if (((int) optDirections[i])*c1Value < ((int) optDirections[i])*c2Value)
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
                double c1Value = 0.0;
                double c2Value = (numObjectives - 1)*alpha + 1;
                for (int j = 0; j < numObjectives; j++)
                {
                    if (j == i) c1Value += c1.objectives[j]/c2.objectives[j];
                    else c1Value += alpha*c1.objectives[j]/c2.objectives[j];
                }
                if (((int) optDirections[i])*c1Value < ((int) optDirections[i])*c2Value)
                    return false;
            }
            return true;
        }
    }

}
