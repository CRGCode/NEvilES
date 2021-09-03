using NEvilES.Abstractions.Pipeline;

namespace NEvilES.Tests.CommonDomain.Sample.ReadModel
{
    public class Dashboard : IHaveIdentity<string>
    {
        public string Id { get; set; }
    }
}