namespace MGroup.Solvers.MachineLearning.StochasticExtensions.RandomNumberGeneration
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class RepeatableRandom : Random
	{
		private List<int> callsNextBytes = new List<int>();
		private List<int> callsNextBytesSpan = new List<int>();
		private List<int> callsNextIntMax = new List<int>();
		private List<(int min, int max)> callsNextIntMinMax = new List<(int min, int max)>();
		private int numCallsNextInt = 0;
		private int numCallsNextDouble = 0;
		private bool recordEverything = true;
		private int seed;

		public RepeatableRandom() : this(Environment.TickCount)
		{
		}

		public RepeatableRandom(int seed) : base(seed)
		{
			this.seed = seed;
		}

		public bool RecordEverything 
		{ 
			get => recordEverything; 
			set
			{
				recordEverything = value;
				if (recordEverything == false)
				{
					callsNextIntMax.Clear();
					callsNextIntMinMax.Clear();
					callsNextBytes.Clear();
					callsNextBytesSpan.Clear();
				}
			}
		}

		public int Seed => seed;

		public State ExtractState()
		{
			var state = new State();
			state.Seed = this.seed;
			state.RecordEverything = recordEverything;
			state.NumCallsNextInt = numCallsNextInt;
			state.NumCallsNextDouble = numCallsNextDouble;

			//TODO: deep copy the lists
			state.CallsNextIntMax = this.callsNextIntMax;
			state.CallsNextIntMinMax = this.callsNextIntMinMax;
			state.CallsNextBytes = this.callsNextBytes;
			state.CallsNextBytesSpan = this.callsNextBytesSpan;

			return state;
		}

		public void LoadState(State state)
		{
			this.seed = state.Seed;

			// Do not depend on the state's flag to reproduce the stored call arguments, as it could have been changed during
			// the lifetime of the original object. Instead do not record optional ones, but copy them from the state.
			this.recordEverything = false;

			this.numCallsNextInt = 0;
			for (int i = 0; i < state.NumCallsNextInt; i++)
			{
				this.Next();
			}

			this.numCallsNextDouble = 0;
			for (int i = 0; i < state.NumCallsNextDouble; i++)
			{
				this.NextDouble();
			}

			this.callsNextIntMax = state.CallsNextIntMax;
			foreach (int maxValue in state.CallsNextIntMax)
			{
				this.Next(maxValue);
			}

			this.callsNextIntMinMax = state.CallsNextIntMinMax;
			foreach ((int minValue, int maxValue) in state.CallsNextIntMinMax)
			{
				this.Next(minValue, maxValue);
			}

			this.callsNextBytes = state.CallsNextBytes;
			foreach (int bufferLength in state.CallsNextBytes)
			{
				var buffer = new byte[bufferLength];
				this.NextBytes(buffer);
			}

			this.callsNextBytesSpan = state.CallsNextBytesSpan;
			foreach (int bufferLength in state.CallsNextBytesSpan)
			{
				Span<byte> buffer = new byte[bufferLength];
				this.NextBytes(buffer);
			}

			// Finally set this to record or not, based on the final flag of the original object.
			this.recordEverything = state.RecordEverything;
		}

		public override int Next()
		{
			numCallsNextInt++;
			return base.Next();
		}

		public override int Next(int maxValue)
		{
			if (recordEverything)
			{
				callsNextIntMax.Add(maxValue);
			}
			return base.Next(maxValue);
		}

		public override int Next(int minValue, int maxValue)
		{
			if (recordEverything)
			{
				callsNextIntMinMax.Add((minValue, maxValue));
			}
			return base.Next(maxValue);
		}

		public override void NextBytes(byte[] buffer)
		{
			if (recordEverything)
			{
				callsNextBytes.Add(buffer.Length);
			}
			base.NextBytes(buffer);
		}

		public override void NextBytes(Span<byte> buffer)
		{
			if (recordEverything)
			{
				callsNextBytesSpan.Add(buffer.Length);
			}
			base.NextBytes(buffer);
		}
		
		public override double NextDouble()
		{
			numCallsNextDouble++;
			return base.NextDouble();
		}

		[Serializable]
		public class State
		{
			public int Seed { get; set; }
			public List<int> CallsNextBytes { get; set; }
			public List<int> CallsNextBytesSpan { get; set; }
			public List<int> CallsNextIntMax { get; set; }
			public List<(int min, int max)> CallsNextIntMinMax { get; set; }
			public int NumCallsNextInt { get; set; }
			public int NumCallsNextDouble { get; set; }
			public bool RecordEverything { get; set; }
		}
	}
}
