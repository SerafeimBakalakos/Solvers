namespace MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptIn)]
	public class Conv1DLayer : IKerasLayer
	{
		public Conv1DLayer(int filters, int kernelSize, int strides, string padding)
		{
			Filters = filters;
			KernelSize = kernelSize;
			Strides = strides;
			Padding = padding;

			if (padding != "same")
			{
				throw new NotImplementedException("Padding can only be \"same\" for now");
			}
		}

		[JsonProperty] public string Name => "Conv1D";

		[JsonProperty] public int Filters { get; }

		[JsonProperty] public int KernelSize { get; }

		[JsonProperty] public int Strides { get; }

		[JsonProperty] public string Padding { get; }
	}
}
