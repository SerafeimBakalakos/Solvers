namespace MGroup.Solvers.DDM.FetiDP.CoarseProblem
{
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.Commons;
	using MGroup.Solvers.Assemblers;

	public class FetiDPCoarseProblemMatrixDense : IFetiDPCoarseProblemGlobalMatrix
	{
		private readonly DenseMatrixAssembler_temp assembler = new DenseMatrixAssembler_temp();
		private Matrix inverseSccGlobal;

		public void Clear() { }

		public void InvertGlobalScc(int numGlobalCornerDofs, IDictionary<int, int[]> subdomainToGlobalCornerDofs, 
			IDictionary<int, IMatrix>  subdomainMatricesScc)
		{
			Matrix globalScc = 
				assembler.BuildGlobalMatrix(numGlobalCornerDofs, subdomainToGlobalCornerDofs, subdomainMatricesScc);
			globalScc.InvertInPlace();
			inverseSccGlobal = globalScc;
		}

		public void MultiplyInverseScc(Vector input, Vector output)
		{
			inverseSccGlobal.MultiplyIntoResult(input, output);
		}

		public DofPermutation ReorderGlobalCornerDofs(int numGlobalCornerDofs, 
			IDictionary<int, int[]> subdomainToGlobalCornerDofs)
			=> DofPermutation.CreateNoPermutation();
	}
}
