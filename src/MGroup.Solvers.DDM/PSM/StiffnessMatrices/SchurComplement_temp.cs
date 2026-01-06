//namespace MGroup.Solvers.DDM.PSM.StiffnessMatrices
//{
//	using System;
//	using System.Collections.Generic;
//	using System.Linq;
//	using System.Text;
//	using System.Threading.Tasks;

//	using MGroup.LinearAlgebra.Matrices;
//	using MGroup.LinearAlgebra.Vectors;
//	using MGroup.Solvers.DDM.PSM.Dofs;

//	internal class SchurComplement_temp : DefaultMatrix
//	{
//		private readonly PsmSubdomainDofs_v2 psmSubdomainDofs;
//		private readonly IPsmSubdomainMatrixManager_v2 psmSubdomainMatrices;

//		public SchurComplement_temp(PsmSubdomainDofs_v2 psmSubdomainDofs, IPsmSubdomainMatrixManager_v2 psmSubdomainMatrices)
//		{
//			this.psmSubdomainDofs = psmSubdomainDofs;
//			this.psmSubdomainMatrices = psmSubdomainMatrices;
//		}

//		public override double this[int rowIdx, int colIdx]
//		{
//			get => throw new NotImplementedException();
//			set => throw new NotImplementedException();
//		}

//		public override int NumColumns => psmSubdomainDofs.DofsBoundaryToAll.Length;

//		public override int NumRows => NumColumns;

//		public override void Clear() => psmSubdomainMatrices.ClearSubMatrices();

//		public override IMatrix CreateZeroMatrixWithSameFormat() => throw new NotImplementedException();

//		public override bool HasSameFormat(IReadOnlyMatrix otherMatrix)
//		{
//			if (otherMatrix is SchurComplement_temp casted)
//			{
//				return (casted.psmSubdomainDofs == this.psmSubdomainDofs) && (casted.psmSubdomainMatrices == this.psmSubdomainMatrices);
//			}

//			return false;
//		}

//		public override void MultiplyIntoResult(IReadOnlyVector lhsVector, IVector rhsVector, bool transposeThis = false)
//		{
//			if (transposeThis)
//			{
//				throw new NotImplementedException();
//			}

//			var lhs = (Vector)lhsVector;
//			var rhs = (Vector)rhsVector;
//			psmSubdomainMatrices.MultiplySchurComplementImplicitly(lhs, rhs);
//		}
//	}
//}
