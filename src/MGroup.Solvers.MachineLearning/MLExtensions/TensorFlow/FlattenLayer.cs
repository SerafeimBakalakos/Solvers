namespace MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptIn)]
	public class FlattenLayer : IKerasLayer
	{
		[JsonProperty] public string Name => "Flatten";
	}
}
