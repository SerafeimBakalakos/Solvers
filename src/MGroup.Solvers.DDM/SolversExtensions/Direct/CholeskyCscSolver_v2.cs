namespace MGroup.Solvers.DDM.SolversExtensions.Direct
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Triangulation;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.DDM.SolversExtensions.Assemblers;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class CholeskyCscSolver_v2 : ISubstructureSystemSolver, IDisposable
	{
		private readonly IImplementationProvider laImplementation;
		private readonly SymmetricCscMatrixAssembler_v2 matrixAssembler = new SymmetricCscMatrixAssembler_v2();
		private readonly DenseVectorAssembler vectorAssembler = new DenseVectorAssembler();

		private ICholeskySymmetricCsc factorization;

		public CholeskyCscSolver_v2(ISubstructure substructure, IImplementationProvider laImplementation)
		{
			this.Substructure = substructure;
			this.laImplementation = laImplementation;
		}

		~CholeskyCscSolver_v2()
		{
			ReleaseResources();
		}

		public void Dispose()
		{
			ReleaseResources();
			GC.SuppressFinalize(this);
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public ISubstructureDofOrdering DofOrdering { get; set; }

		public ISolverLogger Logger { get; }

		public IMatrix Matrix { get; set; }

		public IVector Rhs { get; set; }

		public IVector Solution { get; set; }

		public ISubstructure Substructure { get; set; }

		public void PrepareDofs()
		{
			DofOrdering.PrepareDofMaps();
		}

		public void PrepareLinearSystem()
		{
			Matrix = matrixAssembler.BuildSubstructureMatrix(Substructure, DofOrdering);
			Rhs = vectorAssembler.BuildSubstructureVector(Substructure, DofOrdering);
		}

		public void SolveLinearSystem()
		{
			var watch = new Stopwatch();
			if (Solution == null)
			{
				Solution = Rhs.CreateZeroVectorWithSameFormat();
			}
			else
			{
				Solution.Clear();
			}

			var systemMatrix = (SymmetricCscMatrix)Matrix;
			var systemRhs = (Vector)Rhs;
			var systemSolution = (Vector)Solution;

			// Factorization
			if (factorization == null)
			{
				watch.Start();
				factorization = laImplementation.CreateCholeskyTriangulation();
				factorization.Factorize(systemMatrix);
				watch.Stop();
				Logger.LogTaskDuration("Matrix factorization", watch.ElapsedMilliseconds);
				watch.Reset();
			}

			// Substitutions
			watch.Start();
			factorization.SolveLinearSystem(systemRhs, systemSolution);
			watch.Stop();
			Logger.LogTaskDuration("Back/forward substitutions", watch.ElapsedMilliseconds);
			Logger.IncrementAnalysisStep();
		}

		private void ReleaseResources()
		{
			if (factorization != null)
			{
				factorization.Dispose();
				factorization = null;
			}
		}
	}
}
