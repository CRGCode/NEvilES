namespace NEvilES.Tests.Sample
{
    public class TaxRuleEngine
    {
        public decimal Calculate(decimal grossAmount)
        {
            return grossAmount * 0.23M;
        }
    }
}