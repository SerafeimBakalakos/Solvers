namespace MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptIn)]
	public class LeakyReLULayer : IKerasLayer
	{
		[JsonProperty] public string Name => "LeakyReLU";
	}
}
