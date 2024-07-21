namespace MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptIn)]
	public class ReshapeLayer : IKerasLayer
	{
		public ReshapeLayer(int[] targetShape)
		{
			TargetShape = targetShape;

			if (targetShape.Length != 2)
			{
				throw new NotImplementedException("Target shape can only be a 2D array for now");
			}
		}

		[JsonProperty] public string Name => "Reshape";

		[JsonProperty] public int[] TargetShape { get; }
	}
}
