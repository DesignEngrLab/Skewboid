using System;
using System.Collections.Generic;
using System.Linq;

namespace Skewboid
{
    internal static class ParetoFunctions
    {
        private const double alphaTolerance = 0.0001; // tolerance


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
            IList<double> weights, double tolerance, bool keepEquals, IList<OptimizeDirection> optDirections = null)
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
            var paretoSet = FindParetoCandidates(candidates, 0, optDirections, weights, tolerance, keepEquals);
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
                /* too many in the real pareto - need to filter */
                alphaLB = 0;
                numatLB = numTarget;
                alphaUB = 1;
                paretoSet = FindParetoCandidates(candidates, alphaUB, optDirections, weights, tolerance, keepEquals);
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
            while (numKeep != numTarget && alphaUB - alphaLB > alphaTolerance)
            {
                k++;
                alphaTarget = alphaLB + (alphaUB - alphaLB) * (numatLB - numKeep) / (numatLB - numatUB);
                paretoSet = FindParetoCandidates(candidates, alphaTarget, optDirections, weights, tolerance, keepEquals);
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
        /// Find the Pareto candidates following the Diversity Skewboid method with a given alpha value.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static List<T> FindParetoCandidatesDiverse<T>(IEnumerable<T> candidates, double alpha, double tolerance, bool keepEquals)
            where T : ICandidate
        {
            var numObjectives = candidates.First().Objectives.Count();
            return FindParetoCandidates(candidates, alpha, Enumerable.Repeat(OptimizeDirection.Minimize, numObjectives).ToArray(),
               null, tolerance, keepEquals);
        }
        /// <summary>
        /// Find the Pareto candidates following the Diversity Skewboid method with a given alpha value.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="alpha"></param>
        /// <param name="optDirections"></param>
        /// <returns></returns>
        public static List<T> FindParetoCandidatesDiverse<T>(IEnumerable<T> candidates, double alpha, IList<OptimizeDirection> optDirections, double tolerance, bool keepEquals)
            where T : ICandidate
        => FindParetoCandidates(candidates, alpha, optDirections, null, tolerance, keepEquals);


        /// <summary>
        /// Find the Pareto candidates following the Skewboid method with a given alpha value.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, double alpha, double tolerance, bool keepEquals)
            where T : ICandidate
        {
            var numObjectives = candidates.First().Objectives.Count();
            return FindParetoCandidates(candidates, alpha,
                Enumerable.Repeat(OptimizeDirection.Minimize, numObjectives).ToArray(),
                Enumerable.Repeat(1.0, numObjectives).ToArray(), tolerance, keepEquals);
        }
        /// <summary>
        /// Find the Pareto candidates following the Skewboid method with a given alpha value.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="alpha"></param>
        /// <param name="optDirections"></param>
        /// <returns></returns>
        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, double alpha, IList<OptimizeDirection> optDirections, double tolerance, bool keepEquals)
            where T : ICandidate
        => FindParetoCandidates(candidates, alpha, optDirections, Enumerable.Repeat(1.0, optDirections.Count).ToArray(), tolerance, keepEquals);

        /// <summary>
        /// Find the Pareto candidates following the Skewboid method with a given alpha value. If weights is null, then the method will use the Diversity Skewboid method.
        /// </summary>
        /// <param name="candidates"></param>
        /// <param name="alpha"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, double alpha, IList<double> weights, double tolerance, bool keepEquals)
            where T : ICandidate
        => FindParetoCandidates(candidates, alpha, Enumerable.Repeat(OptimizeDirection.Minimize, weights.Count).ToArray(), weights, tolerance, keepEquals);

        /// <summary>
        /// Find the Pareto candidates following the Skewboid method with a given alpha value. If weights is null, 
        /// then the method will use the Diversity Skewboid method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="candidates"></param>
        /// <param name="alpha"></param>
        /// <param name="optDirections"></param>
        /// <param name="weights"></param>
        /// <returns></returns>
        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, double alpha, IList<OptimizeDirection> optDirections,
            IList<double> weights, double tolerance, bool keepEquals)
            where T : ICandidate
        {
            var paretoSet = new List<T>();
            if (weights != null)
                foreach (var c in candidates)
                    UpdateParetoWithWeights(paretoSet, c, alpha, optDirections, weights, tolerance, keepEquals, out _);
            else
                foreach (var c in candidates)
                    UpdateParetoDiversity(paretoSet, c, alpha, optDirections, tolerance, keepEquals, out _);

            return paretoSet;
        }

        /// <summary>
        /// Finds the pareto candidates (no skewboid, no weights - the OG).
        /// </summary>
        /// <param name="candidates">The candidates.</param>
        /// <param name="_optDirections">The _opt directions.</param>
        /// <returns></returns>
        public static List<T> FindParetoCandidates<T>(IEnumerable<T> candidates, double tolerance, bool keepEquals,
            IList<OptimizeDirection> optDirections = null)
            where T : ICandidate
        {
            var paretoSet = new List<T>();
            foreach (var c in candidates)
                UpdatePareto(paretoSet, c, optDirections, tolerance, keepEquals, out _);
            return paretoSet;
        }

        /// <summary>
        /// Updates the pareto set with the given candidate. Returns true if the candidate is added to the pareto set.
        /// </summary>
        /// <param name="paretoSet"></param>
        /// <param name="c"></param>
        /// <param name="optDirections"></param>
        public static bool UpdatePareto<T>(List<T> paretoSet, T c, IList<OptimizeDirection> optDirections, double tolerance,
            bool keepEquals, out bool equalsExisting)
            where T : ICandidate
        {
            equalsExisting = false;
            for (int i = paretoSet.Count - 1; i >= 0; i--)  // go backwards so that we can remove items
            {
                var pc = paretoSet[i];
                var decision = Dominates(c, pc, optDirections, out var equal, tolerance);
                if (decision == 1)
                    // dominates the existing candidate, so remove the existing candidate
                    paretoSet.Remove(pc);
                else if (decision == -1)
                    // dominated by the existing candidate, so return false
                    return false;
                if (equal)
                {   // if it is equal to the existing candidate, then we can abort the loop
                    equalsExisting = true;
                    if (keepEquals) // if we keep the equals then we add it to the pareto set
                                    // next to the existing candidate so that post-processing can run in O(n)
                        paretoSet.Insert(i, c);
                    return keepEquals;
                }
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
        public static bool UpdateParetoDiversity<T>(List<T> paretoSet, T c, double alpha, IList<OptimizeDirection> optDirections, double tolerance,
            bool keepEquals, out bool equalsExisting)
            where T : ICandidate
        {
            var numObjectives = c.Objectives.Count();
            equalsExisting = false;
            for (int i = paretoSet.Count - 1; i >= 0; i--)
            {
                var pc = paretoSet[i];
                if (CheckIfEqual(pc, c, tolerance))
                {   // if it is equal to the existing candidate, then we can abort the loop
                    equalsExisting = true;
                    if (keepEquals) // if we keep the equals then we add it to the pareto set
                                    // next to the existing candidate so that post-processing can run in O(n)
                        paretoSet.Insert(i, c);
                    return keepEquals;
                }
                if (DominatesDiversity(c, pc, alpha, optDirections, numObjectives))
                    paretoSet.Remove(pc);
                else if (DominatesDiversity(pc, c, alpha, optDirections, numObjectives))
                    return false;
            }
            paretoSet.Add(c);
            return true;
        }

        private static bool CheckIfEqual(ICandidate c1, ICandidate c2, double tolerance)
        {
            var c2Enumerator = c2.Objectives.GetEnumerator();
            foreach (var obj1 in c1.Objectives)
            {
                c2Enumerator.MoveNext();
                var obj2 = (double)c2Enumerator.Current;
                if (2 * Math.Abs(obj1 - obj2) > tolerance * (Math.Abs(obj1) + Math.Abs(obj2)))
                    return false;
            }
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
        public static bool UpdateParetoWithWeights<T>(List<T> paretoSet, T c, double alpha, IList<OptimizeDirection> optDirections, IList<double> weights, double tolerance,
            bool keepEquals, out bool equalsExisting)
            where T : ICandidate
        {
            var numObjectives = c.Objectives.Count();
            equalsExisting = false;
            for (int i = paretoSet.Count - 1; i >= 0; i--)  // go backwards so that we can remove items
            {
                var pc = paretoSet[i];
                var decision = DominatesWithWeights(c, pc, alpha, optDirections, weights, out var equal, tolerance, numObjectives);
                if (decision == 1)
                    // dominates the existing candidate, so remove the existing candidate
                    paretoSet.Remove(pc);
                else if (decision == -1)
                    // dominated by the existing candidate, so return false
                    return false;
                if (equal)
                {   // if it is equal to the existing candidate, then we can abort the loop
                    equalsExisting = true;
                    if (keepEquals) // if we keep the equals then we add it to the pareto set
                                    // next to the existing candidate so that post-processing can run in O(n)
                        paretoSet.Insert(i, c);
                    return keepEquals;
                }
            }
            paretoSet.Add(c);
            return true;
        }

        /// <summary>
        /// Does c1 dominate c2?
        /// </summary>
        /// <param name="c1">the subject candidate, c1 (does this dominate...).</param>
        /// <param name="c2">the object candidate, c2 (is dominated by).</param>
        /// <returns></returns>
        private static int DominatesWithWeights(ICandidate c1, ICandidate c2, double alpha, IList<OptimizeDirection> optDirections, IList<double> weights,
            out bool equal, double tolerance, int numObjectives)
        {
            var c1Dominates = true;
            var c2Dominates = true;
            equal = true;
            // unlike the conventional pareto, we need to cycle over the objectives multiple times, so we need to be
            // able to access the objectives by indexer
            var c1Objectives = c1.Objectives as IList<double> ?? c1.Objectives.ToArray();
            var c2Objectives = c2.Objectives as IList<double> ?? c2.Objectives.ToArray();
            for (int i = 0; i < numObjectives; i++)
            {
                double c1Value = 0.0, c2Value = 0.0;
                for (int j = 0; j < numObjectives; j++)
                {
                    var obj1 = c1Objectives[j];
                    var obj2 = c2Objectives[j];
                    var weight = weights[j];
                    if (2 * Math.Abs(obj1 - obj2) < tolerance * (Math.Abs(obj1) + Math.Abs(obj2)))
                    {
                        equal &= true;
                        continue;
                    }
                    else equal = false;
                    var dir = (int)optDirections[j];
                    if (j == i)
                    {
                        c1Value += dir * weight * obj1;
                        c2Value += dir * weight * obj2;
                    }
                    else
                    {
                        c1Value += dir * alpha * weight * obj1;
                        c2Value += dir * alpha * weight * obj2;
                    }
                }
                if (c1Value > c2Value)
                {
                    c1Dominates &= true; c2Dominates = false;
                }
                else
                {
                    c2Dominates &= true; c1Dominates = false;
                }
            }
            if (c1Dominates) return 1;
            if (c2Dominates) return -1;
            return 0;
        }

        /// <summary>
        /// Does c1 dominate c2? Unlike the weights version, we calculate the score differently if asking does c1 dominate c2 or does c2 dominate c1.
        /// So, this is a simpler function but it is called twice.
        /// </summary>
        /// <param name="c1">the subject candidate, c1 (does this dominate...).</param>
        /// <param name="c2">the object candidate, c2 (is dominated by).</param>
        /// <returns></returns>
        /// <returns></returns>
        private static bool DominatesDiversity(ICandidate c1, ICandidate c2, double alpha, IList<OptimizeDirection> optDirections, int numObjectives)
        {
            var dominates = false;
            var c1Objectives = c1.Objectives as IList<double> ?? c1.Objectives.ToArray();
            var c2Objectives = c2.Objectives as IList<double> ?? c2.Objectives.ToArray();
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
        private static int Dominates(ICandidate c1, ICandidate c2, IList<OptimizeDirection> optDirections,
            out bool equal, double tolerance)
        {
            var c2Enumerator = c2.Objectives.GetEnumerator();
            var i = 0;
            var c1Dominates = true;
            var c2Dominates = true;
            equal = true;
            foreach (var obj1 in c1.Objectives)
            {
                c2Enumerator.MoveNext();
                var obj2 = (double)c2Enumerator.Current;
                // the equality error is the absolute value of the difference divided by the average of the two values
                // to minimize divisions, we move the 2 to the denominator and the sum to the other side of the equation
                if (2 * Math.Abs(obj1 - obj2) < tolerance * (Math.Abs(obj1) + Math.Abs(obj2)))
                {
                    equal &= true;
                    continue;
                }
                else equal = false;
                var dir = (int)optDirections[i++];
                if (dir * obj1 > dir * obj2)
                {
                    c1Dominates &= true; c2Dominates = false;
                }
                else
                {
                    c2Dominates &= true; c1Dominates = false;
                }
            }
            if (c1Dominates) return 1;
            if (c2Dominates) return -1;
            return 0;
        }

    }

}
