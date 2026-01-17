namespace MGroup.Solvers.DDM.PSM.StiffnessMatrices
{
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.DDM.LinearSystem;
	using MGroup.Solvers.DDM.PSM.Dofs;
	using MGroup.Solvers.Assemblers;

	public interface IPsmSubdomainMatrixManagerFactory_v2<TMatrix>
		where TMatrix : class, IMatrix
	{
		IDomainMatrixAssembler_v2<TMatrix> CreateAssembler();

		IPsmSubdomainMatrixManager_v2 CreateMatrixManager(
			IImplementationProvider provider, SubdomainLinearSystem_v2<TMatrix> subLinearSystem, PsmSubdomainDofs_v2 subdomainDofs);
	}
}
