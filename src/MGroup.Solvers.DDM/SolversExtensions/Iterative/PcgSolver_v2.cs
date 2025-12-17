namespace MGroup.Solvers.DDM.SolversExtensions.Iterative
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml.Linq;

	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Reordering;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.Assemblers;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class PcgSolver_v2 : ISubdomainSystemSolver
	{
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly bool matrixPatternWillNotBeModified = false;
		private readonly CsrMatrixAssembler_v2 matrixAssembler = new CsrMatrixAssembler_v2();

		private readonly IPreconditioner preconditioner;
		private bool mustUpdatePreconditioner = true;

		public PcgSolver_v2(ISubdomain_v2 subdomain, PcgAlgorithm pcgAlgorithm, IPreconditioner preconditioner)
		{
			Subdomain = subdomain;
			this.pcgAlgorithm = pcgAlgorithm;
			this.preconditioner = preconditioner;
			DofOrdering = new DefaultSubdomainDofOrdering(subdomain, null);
			LinearSystem = new LinearSystem_v2();
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public ISubdomainDofOrdering_v2 DofOrdering { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; }

		public ISubdomain_v2 Subdomain { get; }

		public IAlgebraicModel_v2 CreateAlgebraicModel(IModel_v2 physicalModel)
		{
			return new SharedMemoryAlgebraicModel_v2(physicalModel, DofOrdering);
		}

		public void PrepareDofs()
		{
			DofOrdering.OrderDofs();
			DofOrdering.PrepareDofMaps();
			LinearSystem.RhsVector = Vector.CreateZero(DofOrdering.NumDofs);
		}

		public void BuildSystemMatrix()
		{
			LinearSystem.Matrix = matrixAssembler.BuildSubdomainMatrix(Subdomain, DofOrdering);
		}

		public void SolveLinearSystem()
		{
			var watch = new Stopwatch();
			if (LinearSystem.Solution == null)
			{
				LinearSystem.Solution = LinearSystem.RhsVector.CreateZeroVectorWithSameFormat();
			}
			else
			{
				LinearSystem.Solution.Clear();
			}

			// Preconditioning
			if (mustUpdatePreconditioner)
			{
				watch.Start();
				preconditioner.UpdateMatrix(LinearSystem.Matrix, !matrixPatternWillNotBeModified);
				mustUpdatePreconditioner = false;
				watch.Stop();
				Logger.LogTaskDuration("Calculating preconditioner", watch.ElapsedMilliseconds);
				watch.Reset();
			}

			// Iterative algorithm
			watch.Start();
			IterativeStatistics stats = pcgAlgorithm.Solve(LinearSystem.Matrix, preconditioner, LinearSystem.RhsVector, LinearSystem.Solution, true);
			if (!stats.HasConverged)
			{
				throw new IterativeSolverNotConvergedException(typeof(PcgSolver_v2).Name
					+ $" did not converge to a solution. PCG algorithm run for {stats.NumIterationsRequired} iterations and the residual norm ratio was {stats.ResidualNormRatioEstimation}");
			}

			watch.Stop();
			Logger.LogTaskDuration("Iterative algorithm", watch.ElapsedMilliseconds);
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
			Logger.IncrementAnalysisStep();
		}
	}
}
