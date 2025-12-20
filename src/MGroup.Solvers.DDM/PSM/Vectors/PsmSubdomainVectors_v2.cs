namespace MGroup.Solvers.DDM.PSM.Vectors
{
	using System;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.LinearSystem;
	using MGroup.Solvers.DDM.PSM.Dofs;
	using MGroup.Solvers.DDM.PSM.StiffnessMatrices;
	using MGroup.Solvers.DiscretizationExtensions;

	public class PsmSubdomainVectors_v2
	{
		private readonly IPsmSubdomainMatrixManager_v2 matrixManagerPsm;
		private readonly PsmSubdomainDofs_v2 subdomainDofs;
		private readonly ISubdomainLinearSystem_v2 subLinearSystem;

		private Vector vectorFb;
		private Vector vectorFi;

		public PsmSubdomainVectors_v2(ISubdomainLinearSystem_v2 subLinearSystem, PsmSubdomainDofs_v2 subdomainDofs, IPsmSubdomainMatrixManager_v2 matrixManagerPsm)
		{
			this.subLinearSystem = subLinearSystem;
			this.subdomainDofs = subdomainDofs;
			this.matrixManagerPsm = matrixManagerPsm;
		}

		public bool IsEmpty => vectorFi == null;

		public Vector CalcCondensedRhsVector()
		{
			// Static condensation: fbCondensed[s] = fb[s] - Kbi[s] * inv(Kii[s]) * fi[s]
			Vector temp = matrixManagerPsm.MultiplyInverseKii(vectorFi);
			temp = matrixManagerPsm.MultiplyKbi(temp);
			Vector fbCondensed = vectorFb - temp;

			return fbCondensed;
		}

		public void Clear()
		{
			vectorFi = null;
		}

		public void ExtractBoundaryInternalRhsVectors(Action<Vector> scaleBoundaryVector)
		{
			int[] internalDofs = subdomainDofs.DofsInternalToAll;
			int[] boundaryDofs = subdomainDofs.DofsBoundaryToAll;
			Vector f = subLinearSystem.RhsVector;

			this.vectorFi = f.GetSubvector(internalDofs);
			this.vectorFb = f.GetSubvector(boundaryDofs);
			scaleBoundaryVector(vectorFb);
		}

		public Vector CalcSubdomainSolution(Vector subdomainBoundarySolution)
		{
			int numAllDofs = subLinearSystem.RhsVector.Length;
			int[] boundaryDofs = subdomainDofs.DofsBoundaryToAll;
			int[] internalDofs = subdomainDofs.DofsInternalToAll;

			// ui[s] = inv(Kii[s]) * (fi[s] - Kib[s] * ub[s])
			Vector ub = subdomainBoundarySolution;
			Vector temp = matrixManagerPsm.MultiplyKib(ub);
			temp.LinearCombinationIntoThis(-1.0, vectorFi, +1);
			Vector ui = matrixManagerPsm.MultiplyInverseKii(temp);

			// Gather ub[s], ui[s] into uf[s]
			var u = Vector.CreateZero(numAllDofs);
			u.CopyNonContiguouslyFrom(boundaryDofs, subdomainBoundarySolution);
			u.CopyNonContiguouslyFrom(internalDofs, ui);

			return u;
		}

		public void CalcStoreSubdomainSolution(Vector subdomainBoundarySolution)
		{
			subLinearSystem.Solution = CalcSubdomainSolution(subdomainBoundarySolution);
		}
	}
}
