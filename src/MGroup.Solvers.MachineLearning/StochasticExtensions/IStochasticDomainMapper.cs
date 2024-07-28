namespace MGroup.Solvers.MachineLearning.StochasticExtensions
{ 
	/// <summary>The interface of the classes that map values from the stochastic
	/// domain generated from the stochastic coefficients provider to the problem domain. </summary>
	public interface IStochasticDomainMapper
    {
        double[] Map (double[] problemDomainVector);
    }
}
