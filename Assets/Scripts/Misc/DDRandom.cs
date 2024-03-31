using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace Wozware.Downslope
{
	/// <summary> 
	/// Discrete Distributed Random. Used to assign weights to random choices.
	/// Uses List.BinarySearch to determine next value.
	/// </summary>
	public sealed class DDRandom
	{
		private List<int> m_accumulatedWeights;
		private int m_totalWeight;
		private Random m_rnd;

		public DDRandom(IEnumerable<int> weights, Random rnd = null)
		{
			int accumulator = 0;
			m_accumulatedWeights = weights.Select(
				(int prob) => {
					int output = accumulator;
					accumulator += prob;
					return output;
				}
			).ToList();

			m_totalWeight = accumulator;
			m_rnd = (rnd != null) ? rnd : new Random();
		}

		public DDRandom(Random rnd, params int[] weights) :
			this(weights, rnd)
		{ }

		public DDRandom(params int[] weights) :
			this(weights, null)
		{ }

		public int Next()
		{
			int index = m_accumulatedWeights.BinarySearch(m_rnd.Next(m_totalWeight));
			return (index >= 0) ? index : ~index - 1;
		}
	}
}
