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
	using MGroup.LinearAlgebra.Reordering;
	using MGroup.LinearAlgebra.Triangulation;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.DDM.SolversExtensions.Assemblers;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;
	using MGroup.Solvers.DofOrdering.Reordering;

	public class CholeskyCscSolver_v2 : ISubdomainSystemSolver, IDisposable
	{
		private readonly IImplementationProvider laImplementation;
		private readonly SymmetricCscMatrixAssembler_v2 matrixAssembler = new SymmetricCscMatrixAssembler_v2();

		private ICholeskySymmetricCsc factorization;

		public CholeskyCscSolver_v2(ISubdomain_v2 subdomain, IImplementationProvider laImplementation)
		{
			this.Subdomain = subdomain;
			this.laImplementation = laImplementation;
			var dofOrdering = new DefaultSubdomainDofOrdering(subdomain, new AmdSymmetricOrdering(laImplementation));
			Problem = new SharedMemoryStructureProblem(subdomain, dofOrdering);
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

		public ISolverLogger Logger { get; }

		public ISubdomainProblem Problem { get; }

		public ISubdomain_v2 Subdomain { get; }

		public void PrepareDofs()
		{
			Problem.OrderDofs();
		}

		public void BuildSystemMatrix()
		{
			Problem.SystemMatrix = matrixAssembler.BuildSubdomainMatrix(Subdomain, Problem.DofOrdering);
		}

		public void SolveLinearSystem()
		{
			var watch = new Stopwatch();
			if (Problem.SystemSolution == null)
			{
				Problem.SystemSolution = Problem.SystemRhs.CreateZeroVectorWithSameFormat();
			}
			else
			{
				Problem.SystemSolution.Clear();
			}

			var systemMatrix = (SymmetricCscMatrix)(Problem.SystemMatrix);
			var systemRhs = (Vector)(Problem.SystemRhs);
			var systemSolution = (Vector)(Problem.SystemSolution);

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
