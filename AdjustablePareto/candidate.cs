using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdjustablePareto
{
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
