namespace MGroup.Solvers.MachineLearning.Tests
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow;
	using MGroup.Solvers.MachineLearning.PodAmg.Surrogates;
	using MGroup.Solvers.MachineLearning.Tests.Dynamic;
	using MGroup.Solvers.MachineLearning.Tests.MLExtensions;

	using Newtonsoft.Json;

	public class Program
	{
		public static void Main(string[] args)
		{
			//CallingPythonTests.TestTrainAndPredict(doublePrecision: false, binaryIOFiles: true);
			CantileverDynamicAnalysis_v2.RunStochasticAnalysis();
			//CantileverDynamicAnalysis.RunStochasticAnalysis();
			//CantileverDynamicAnalysis.RunStandAloneAnalysis();
			//TrySerialization();
		}

		private static void TrySerialization()
		{
			int ffnnHiddenSize = 64;

			var arch = new CaeFfnnArchitecture();
			arch.NumDofs = 4719;
			arch.NumModelParams = 2;
			arch.LatentSpaceDim = 8;

			arch.CaeLearningRate = 5E-4f;
			arch.CaeNumEpochs = 40;
			arch.CaeBatchSize = 10;
			arch.FfnnLearningRate = 1E-4f;
			arch.FfnnNumEpochs = 3000;
			arch.FfnnBatchSize = 20;

			arch.EncoderLayers.Add(new Conv1DLayer(filters: 128, kernelSize: 5, strides: 1, padding: "same"));
			arch.EncoderLayers.Add(new LeakyReLULayer());
			arch.EncoderLayers.Add(new Conv1DLayer(filters: 64, kernelSize: 5, strides: 1, padding: "same"));
			arch.EncoderLayers.Add(new LeakyReLULayer());
			arch.EncoderLayers.Add(new Conv1DLayer(filters: 32, kernelSize: 5, strides: 1, padding: "same"));
			arch.EncoderLayers.Add(new LeakyReLULayer());
			arch.EncoderLayers.Add(new Conv1DLayer(filters: 16, kernelSize: 5, strides: 1, padding: "same"));
			arch.EncoderLayers.Add(new LeakyReLULayer());
			arch.EncoderLayers.Add(new FlattenLayer());
			arch.EncoderLayers.Add(new DenseLayer(units: arch.LatentSpaceDim));

			arch.DecoderLayers.Add(new Input1DLayer(arch.LatentSpaceDim));
			arch.DecoderLayers.Add(new DenseLayer(units: 32));
			arch.DecoderLayers.Add(new LeakyReLULayer());
			arch.DecoderLayers.Add(new ReshapeLayer(new int[] { 1, 32 }));
			arch.DecoderLayers.Add(new Conv1DTransposeLayer(filters: 32, kernelSize: 5, strides: 1, padding: "same"));
			arch.DecoderLayers.Add(new LeakyReLULayer());
			arch.DecoderLayers.Add(new Conv1DTransposeLayer(filters: 64, kernelSize: 5, strides: 1, padding: "same"));
			arch.DecoderLayers.Add(new LeakyReLULayer());
			arch.DecoderLayers.Add(new Conv1DTransposeLayer(filters: 128, kernelSize: 5, strides: 1, padding: "same"));
			arch.DecoderLayers.Add(new LeakyReLULayer());
			arch.DecoderLayers.Add(new Conv1DTransposeLayer(filters: arch.NumDofs, kernelSize: 5, strides: 1, padding: "same"));

			arch.FfnnLayers.Add(new Input1DLayer(arch.NumModelParams));
			arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			arch.FfnnLayers.Add(new LeakyReLULayer());
			arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			arch.FfnnLayers.Add(new LeakyReLULayer());
			arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			arch.FfnnLayers.Add(new LeakyReLULayer());
			arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			arch.FfnnLayers.Add(new LeakyReLULayer());
			arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			arch.FfnnLayers.Add(new LeakyReLULayer());
			arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			arch.FfnnLayers.Add(new LeakyReLULayer());
			arch.FfnnLayers.Add(new DenseLayer(units: arch.LatentSpaceDim));

			string path = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonCSharpBridge\\load_keras_model\\cs2py_settings.json";
			using (StreamWriter file = File.CreateText(path))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(file, arch);
			}
		}
	}
}
