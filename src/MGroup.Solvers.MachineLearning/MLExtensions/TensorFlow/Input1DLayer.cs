namespace MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptIn)]
	public class Input1DLayer : IKerasLayer
	{
		public Input1DLayer(int inputShape) 
		{
			InputShape = inputShape;
		}

		[JsonProperty] public string Name => "InputLayer";

		[JsonProperty] public int InputShape { get; }
	}
}
