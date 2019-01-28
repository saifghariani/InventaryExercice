using EventFlow.Core;

namespace Inventary
{
    public class SaleId : Identity<SaleId>
    {
        public SaleId(string id) : base(id) {
        }
    }
}
