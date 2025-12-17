namespace MGroup.Solvers.DDM.PSM.StiffnessMatrices
{
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DDM.LinearSystem;
	using MGroup.Solvers.DDM.PSM.Dofs;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public interface IPsmSubdomainMatrixManagerFactory_v2<TMatrix>
		where TMatrix : class, IMatrix
	{
		ISubdomainMatrixAssembler<TMatrix> CreateAssembler();

		IPsmSubdomainMatrixManager_v2 CreateMatrixManager(
			IImplementationProvider provider, LinearSystem_v2 linearSystem, PsmSubdomainDofs_v2 subdomainDofs);
	}
}
