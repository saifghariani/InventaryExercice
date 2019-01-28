using Newtonsoft.Json;

namespace Inventary
{
    public class Sale
    {
        [JsonConstructor]
        public Sale(SaleId saleId, string productName, int quantity, float price) {
            SaleId=saleId;
            ProductName=productName;
            Quantity=quantity;
            Price=price;
        }

        public SaleId SaleId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public float Price { get; set; }
    }
}
