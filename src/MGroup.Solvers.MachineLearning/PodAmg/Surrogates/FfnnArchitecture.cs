namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	using MGroup.MachineLearning.TensorFlow.KerasLayers;

	using MGroup.MachineLearning.Utilities;
	using MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow;

	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptOut)]
	public class FfnnArchitecture
	{
		public FfnnArchitecture()
		{
		}

		public int NumPodBasisVectors { get; set; } = -1;

		public int NumModelParams { get; set; } = -1;

		public int FfnnBatchSize { get; set; } = -1;

		public int FfnnNumEpochs { get; set; } = -1;

		public float FfnnLearningRateStart { get; set; } = 0.0f;

		public float FfnnLearningRateEnd { get; set; } = 0.0f;

		public List<IKerasLayer> FfnnLayers { get; set; } = new List<IKerasLayer>();
	}
}
