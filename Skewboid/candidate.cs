using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Skewboid
{
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

    internal interface ICandidate
    {
        double[] objectives { get; set; }
    }
    internal class candidate : ICandidate
    {
        public double[] objectives { get; set; }
        internal candidate(double[] _objectives)
        {
            objectives = (double[])_objectives.Clone();
        }
    }
}
