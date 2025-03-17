namespace MGroup.Solvers.MachineLearning.Tests.Dynamic
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;

	public class SingleDofModel
	{
		private readonly double stiffness;
		private readonly double mass;
		private readonly double dampingRatio;
		private readonly double naturalFrequency;
		private bool isExternalLoadRamp = false;
		private double externalLoadMax;
		private double externalLoadFrequency;
		private double externalLoadRampDuration;

		private SingleDofModel(double stiffness, double mass, double dampingRatio)
		{
			this.stiffness = stiffness;
			this.mass = mass;
			this.dampingRatio = dampingRatio;
			this.naturalFrequency = Math.Sqrt(stiffness / mass);
		}

		public double InitialDisplacement { get; set; } = 0;

		public double InitialVelocity { get; set; } = 0;

		public static SingleDofModel CreateWithHarmonicLoad(
			double stiffness, double mass, double dampingRatio, double externalLoadMax, double externalLoadCyclicFrequency)
		{
			var model = new SingleDofModel(stiffness, mass, dampingRatio);
			model.isExternalLoadRamp = false;
			model.externalLoadMax = externalLoadMax;
			model.externalLoadFrequency = externalLoadCyclicFrequency;
			return model;
		}

		public static SingleDofModel CreateWithRampLoad(
			double stiffness, double mass, double dampingRatio, double externalLoadMax, double rampDuration)
		{
			var model = new SingleDofModel(stiffness, mass, dampingRatio);
			model.isExternalLoadRamp = true;
			model.externalLoadMax = externalLoadMax;
			model.externalLoadRampDuration = rampDuration;
			return model;
		}

		public List<Vector> CalcDisplacementHistory(double start, double timeStep, int numTimeSteps)
		{
			if (isExternalLoadRamp)
			{
				throw new NotImplementedException();
			}
			else
			{
				double wn = naturalFrequency;
				double w = externalLoadFrequency;
				double z = dampingRatio;
				double u0 = InitialDisplacement;
				double v0 = InitialVelocity;

				double b = w / wn;
				double wD = wn * Math.Sqrt(1 - z * z);

				double magnificationFactor = 1 / Math.Sqrt(Math.Pow(1 - b * b, 2) + Math.Pow(2 * dampingRatio * b, 2));
				double upMax = externalLoadMax / stiffness * magnificationFactor; // permanent amplitude
				double phi = Math.Atan((2 * dampingRatio * b) / (1 - b * b)); // phase difference

				double A = u0 + upMax * Math.Sin(phi); // constant for cos
				double B = (v0 + z * wn * A - w * upMax * Math.Cos(phi)) / wD; // constant for sin

				var result = new List<Vector>(numTimeSteps);
				for (int i = 0; i < numTimeSteps; i++)
				{
					double t = i * timeStep;
					//double u =
					//	Math.Exp(-z * wn * t) * (A * Math.Cos(wD * t) + B * Math.Sin(wD * t)) + upMax * Math.Sin(w * t - phi);
					//double u = upMax * Math.Sin(w * t - phi);
					double u = Math.Exp(-z * wn * t) * (A * Math.Cos(wD * t) + B * Math.Sin(wD * t));
					result.Add(Vector.CreateFromArray(new double[] { u }));
				}

				return result;
			}
		}
	}
}
