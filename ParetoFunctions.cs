using System;
using System.Collections.Generic;
using System.Linq;

namespace Skewboid
{
    internal static class ParetoFunctions
    {
        private const double tolerance = 0.0001; // tolerance


        /// <summary>
        /// Finds the given number candidates by iterating (root-finding) on alpha.
        /// </summary>
        /// <param name="candidates">The candidates.</param>
        /// <param name="numKeep">The num keep.</param>
        /// <param name="alphaTarget">The alpha target.</param>
        /// <param name="_weights">The _weights.</param>
        /// <param name="_optDirections">The _opt directions.</param>
        /// <returns></returns>
        public static List<T> FindGivenNumCandidates<T>(List<T> candidates, int numKeep, out double alphaTarget,
                                                             IList<double> weights, IList<OptimizeDirection> optDirections = null)
            where T : ICandidate
        {
            var numObjectives = candidates.First().Objectives.Count();
            if (optDirections == null)
            {
                optDirections = new OptimizeDirection[numObjectives];
                for (int i = 0; i < numObjectives; i++)
                    optDirections[i] = OptimizeDirection.Minimize;
            }
            double alphaLB, alphaUB;
            int numatLB, numatUB;
            var paretoSet = FindParetoCandidates(candidates, 0, optDirections, weights);
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
                paretoSet = FindParetoCandidates(candidates, alphaUB, optDirections, weights);
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
                alphaTarget = alphaLB + (alphaUB - alphaLB) * (numatLB - numKeep) / (numatLB - numatUB);
                paretoSet = FindParetoCandidates(candidates, alphaTarget, optDirections, weights);
                numTarget = paretoSet.Count;
                if (numTarget > numKeep)
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

        /// <summary>
        /// Find the Pareto candidates following the Skewboid method with a given alpha value.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, double alpha)
            where T : ICandidate
        {
            var numObjectives = candidates.First().Objectives.Count();
            return FindParetoCandidates(candidates, alpha,
                Enumerable.Repeat(OptimizeDirection.Minimize, numObjectives).ToArray(),
                Enumerable.Repeat(1.0, numObjectives).ToArray());
        }
        /// <summary>
        /// Find the Pareto candidates following the Skewboid method with a given alpha value.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="alpha"></param>
        /// <param name="optDirections"></param>
        /// <returns></returns>
        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, double alpha, IList<OptimizeDirection> optDirections)
            where T : ICandidate
        => FindParetoCandidates(candidates, alpha, optDirections, Enumerable.Repeat(1.0, optDirections.Count).ToArray());

        /// <summary>
        /// Find the Pareto candidates following the Skewboid method with a given alpha value.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="alpha"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, double alpha, IList<double> weights)
            where T : ICandidate
        => FindParetoCandidates(candidates, alpha, Enumerable.Repeat(OptimizeDirection.Minimize, weights.Count).ToArray(), weights);


        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, double alpha, IList<OptimizeDirection> optDirections, IList<double> weights)
            where T : ICandidate
        {
            var paretoSet = new List<T>();
            if (weights != null)
                foreach (var c in candidates)
                    UpdateParetoWithWeights(paretoSet, c, alpha, optDirections, weights);
            else
                foreach (var c in candidates)
                    UpdateParetoDiversity(paretoSet, c, alpha, optDirections);

            return paretoSet;
        }

        /// <summary>
        /// Finds the pareto candidates (no skewboid, no weights - the OG).
        /// </summary>
        /// <param name="candidates">The candidates.</param>
        /// <param name="_optDirections">The _opt directions.</param>
        /// <returns></returns>
        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, IList<OptimizeDirection> optDirections = null)
            where T : ICandidate
        {
            var paretoSet = new List<T>();
            foreach (var c in candidates)
            {
                UpdatePareto(paretoSet, c, optDirections);
            }
            return paretoSet;
        }

        /// <summary>
        /// Updates the pareto set with the given candidate. Returns true if the candidate is added to the pareto set.
        /// </summary>
        /// <param name="paretoSet"></param>
        /// <param name="c"></param>
        /// <param name="optDirections"></param>
        public static bool UpdatePareto<T>(List<T> paretoSet, T c, IList<OptimizeDirection> optDirections)
            where T : ICandidate
        {
            for (int i = paretoSet.Count - 1; i >= 0; i--)
            {
                var pc = paretoSet[i];
                if (Dominates(c, pc, optDirections))
                    paretoSet.Remove(pc);
                else if (Dominates(pc, c, optDirections)) return false;
            }
            paretoSet.Add(c);
            return true;
        }

        /// <summary>
        /// Updates the pareto set with the given candidate using a skewboid alpha and the diversity method. 
        /// Returns true if the candidate is added to the pareto set.
        /// </summary>
        /// <param name="paretoSet"></param>
        /// <param name="c"></param>
        /// <param name="alpha"></param>
        /// <param name="optDirections"></param>
        /// <returns></returns>
        public static bool UpdateParetoDiversity<T>(List<T> paretoSet, T c, double alpha, IList<OptimizeDirection> optDirections)
            where T : ICandidate
        {
            for (int i = paretoSet.Count - 1; i >= 0; i--)
            {
                var pc = paretoSet[i];
                if (DominatesDiversity(c, pc, alpha, optDirections))
                    paretoSet.Remove(pc);
                else if (DominatesDiversity(pc, c, alpha, optDirections)) return false;
            }
            paretoSet.Add(c);
            return true;
        }

        /// <summary>
        /// Updates the pareto set with the given candidate using a skewboid alpha and with weights. 
        /// Returns true if the candidate is added to the pareto set.
        /// </summary>
        /// <param name="paretoSet"></param>
        /// <param name="c"></param>
        /// <param name="alpha"></param>
        /// <param name="optDirections"></param>
        /// <param name="weights"></param>
        public static void UpdateParetoWithWeights<T>(List<T> paretoSet, T c, double alpha, IList<OptimizeDirection> optDirections, IList<double> weights)
            where T : ICandidate
        {
            for (int i = paretoSet.Count - 1; i >= 0; i--)
            {
                var pc = paretoSet[i];
                if (DominatesWithWeights(c, pc, alpha, optDirections, weights))
                    paretoSet.Remove(pc);
                else if (DominatesWithWeights(pc, c, alpha, optDirections, weights)) return;
            }
            paretoSet.Add(c);
        }

        /// <summary>
        /// Does c1 dominate c2?
        /// </summary>
        /// <param name="c1">the subject candidate, c1 (does this dominate...).</param>
        /// <param name="c2">the object candidate, c2 (is dominated by).</param>
        /// <returns></returns>
        private static bool DominatesWithWeights(ICandidate c1, ICandidate c2, double alpha, IList<OptimizeDirection> optDirections, IList<double> weights)
        {
            var dominates = false;
            var c1Objectives = c1.Objectives as IList<double> ?? c1.Objectives.ToArray();
            var c2Objectives = c2.Objectives as IList<double> ?? c2.Objectives.ToArray();
            var numObjectives = c1Objectives.Count;
            for (int i = 0; i < numObjectives; i++)
            {
                double c1Value = 0.0, c2Value = 0.0;
                for (int j = 0; j < numObjectives; j++)
                {
                    if (j == i)
                    {
                        c1Value += (int)optDirections[j] * weights[j] * c1Objectives[j];
                        c2Value += (int)optDirections[j] * weights[j] * c2Objectives[j];
                    }
                    else
                    {
                        c1Value += (int)optDirections[j] * alpha * weights[j] * c1Objectives[j];
                        c2Value += (int)optDirections[j] * alpha * weights[j] * c2Objectives[j];
                    }
                }
                if (c1Value < c2Value) return false;
                if (c1Value > c2Value) dominates = true;
            }
            return dominates;
        }

        /// <summary>
        /// Does c1 dominate c2?
        /// </summary>
        /// <param name="c1">the subject candidate, c1 (does this dominate...).</param>
        /// <param name="c2">the object candidate, c2 (is dominated by).</param>
        /// <returns></returns>
        private static bool DominatesDiversity(ICandidate c1, ICandidate c2, double alpha, IList<OptimizeDirection> optDirections)
        {
            var dominates = false;
            var c1Objectives = c1.Objectives as IList<double> ?? c1.Objectives.ToArray();
            var c2Objectives = c2.Objectives as IList<double> ?? c2.Objectives.ToArray();
            var numObjectives = c1Objectives.Count;
            for (int i = 0; i < numObjectives; i++)
            {
                double c1Value = 0.0, c2Value = 0.0;
                for (int j = 0; j < numObjectives; j++)
                {
                    if (j == i)
                    {
                        c1Value += (int)optDirections[j] * c1Objectives[j] / Math.Abs(c2Objectives[j]);
                        c2Value += (int)optDirections[j] * Math.Sign(c2Objectives[j]);
                    }
                    else
                    {
                        c1Value += (int)optDirections[j] * alpha * c1Objectives[j] / Math.Abs(c2Objectives[j]);
                        c2Value += (int)optDirections[j] * alpha * Math.Sign(c2Objectives[j]);
                    }
                }
                if (c1Value < c2Value) return false;
                if (c1Value > c2Value) dominates = true;
            }
            return dominates;
        }

        /// <summary>
        /// returns true if c1 dominates c2
        /// </summary>
        /// <param name="c1">The c1.</param>
        /// <param name="c2">The c2.</param>
        /// <returns></returns>
        private static bool Dominates(ICandidate c1, ICandidate c2, IList<OptimizeDirection> optDirections)
        {
            var c2Enumerator = c2.Objectives.GetEnumerator();
            var i = 0;
            foreach (var obj1 in c1.Objectives)
            {
                c2Enumerator.MoveNext();
                var obj2 = (double)c2Enumerator.Current;
                if (((int)optDirections[i]) * obj1 < ((int)optDirections[i]) * obj2)
                    return false;
                i++;
            }
            return true;
        }

    }

}
