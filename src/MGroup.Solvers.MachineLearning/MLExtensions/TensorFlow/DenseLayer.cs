namespace MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptIn)]
	public class DenseLayer : IKerasLayer
	{
		public DenseLayer(int units)
		{
			Units = units;
		}

		[JsonProperty] public string Name => "Dense";

		[JsonProperty] public int Units { get; }
	}
}
