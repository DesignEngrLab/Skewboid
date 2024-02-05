using System.Collections.Generic;
using System.Linq;

namespace Skewboid
{
    ///<summary>
    /// Enumerator for Search functions that have generality
    /// to either minimize or maximize (e.g. PNPPS, stochasticChoose). */
    ///</summary>
    internal enum OptimizeDirection
    {
        /// <summary>
        /// Minimize in the search - smaller is better.
        /// </summary>
        Minimize = -1,

        /// <summary>
        /// Maximize in the search - bigger is better.
        /// </summary>
        Maximize = 1
    };

    internal interface ICandidate
    {
        IEnumerable<double> Objectives { get;  }
    }
    internal class DefaultCandidate : ICandidate
    {
        public IEnumerable<double> Objectives => objectives;
        private double[] objectives;
        internal DefaultCandidate(IEnumerable<double> objectives)
        {
            this.objectives = objectives.ToArray();
        }
    }
}
