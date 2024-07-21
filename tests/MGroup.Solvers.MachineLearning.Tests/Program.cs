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
			//TrySerialization();
			CantileverDynamicAnalysis.RunStochasticAnalysis();
			//CantileverDynamicAnalysis.RunStandAloneAnalysis();
		}

		private static void TrySerialization()
		{
			int ffnnHiddenSize = 64;

			var descr = new CaeFfnnDescription();
			descr.NumDofs = 4719;
			descr.NumModelParams = 2;
			descr.LatentSpaceDim = 8;

			descr.CaeLearningRate = 5E-4f;
			descr.CaeNumEpochs = 40;
			descr.CaeBatchSize = 10;
			descr.FfnnLearningRate = 1E-4f;
			descr.FfnnNumEpochs = 3000;
			descr.FfnnBatchSize = 20;

			descr.EncoderLayers.Add(new Conv1DLayer(filters: 128, kernelSize: 5, strides: 1, padding: "same"));
			descr.EncoderLayers.Add(new LeakyReLULayer());
			descr.EncoderLayers.Add(new Conv1DLayer(filters: 64, kernelSize: 5, strides: 1, padding: "same"));
			descr.EncoderLayers.Add(new LeakyReLULayer());
			descr.EncoderLayers.Add(new Conv1DLayer(filters: 32, kernelSize: 5, strides: 1, padding: "same"));
			descr.EncoderLayers.Add(new LeakyReLULayer());
			descr.EncoderLayers.Add(new Conv1DLayer(filters: 16, kernelSize: 5, strides: 1, padding: "same"));
			descr.EncoderLayers.Add(new LeakyReLULayer());
			descr.EncoderLayers.Add(new FlattenLayer());
			descr.EncoderLayers.Add(new DenseLayer(units: descr.LatentSpaceDim));

			descr.DecoderLayers.Add(new Input1DLayer(descr.LatentSpaceDim));
			descr.DecoderLayers.Add(new DenseLayer(units: 32));
			descr.DecoderLayers.Add(new LeakyReLULayer());
			descr.DecoderLayers.Add(new ReshapeLayer(new int[] { 1, 32 }));
			descr.DecoderLayers.Add(new Conv1DTransposeLayer(filters: 32, kernelSize: 5, strides: 1, padding: "same"));
			descr.DecoderLayers.Add(new LeakyReLULayer());
			descr.DecoderLayers.Add(new Conv1DTransposeLayer(filters: 64, kernelSize: 5, strides: 1, padding: "same"));
			descr.DecoderLayers.Add(new LeakyReLULayer());
			descr.DecoderLayers.Add(new Conv1DTransposeLayer(filters: 128, kernelSize: 5, strides: 1, padding: "same"));
			descr.DecoderLayers.Add(new LeakyReLULayer());
			descr.DecoderLayers.Add(new Conv1DTransposeLayer(filters: descr.NumDofs, kernelSize: 5, strides: 1, padding: "same"));

			descr.FfnnLayers.Add(new Input1DLayer(descr.NumModelParams));
			descr.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			descr.FfnnLayers.Add(new LeakyReLULayer());
			descr.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			descr.FfnnLayers.Add(new LeakyReLULayer());
			descr.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			descr.FfnnLayers.Add(new LeakyReLULayer());
			descr.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			descr.FfnnLayers.Add(new LeakyReLULayer());
			descr.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			descr.FfnnLayers.Add(new LeakyReLULayer());
			descr.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
			descr.FfnnLayers.Add(new LeakyReLULayer());
			descr.FfnnLayers.Add(new DenseLayer(units: descr.LatentSpaceDim));

			string path = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonCSharpBridge\\load_keras_model\\cs2py_settings.json";
			using (StreamWriter file = File.CreateText(path))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(file, descr);
			}
		}
	}
}
