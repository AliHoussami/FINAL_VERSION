namespace projet_info_finale.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public int RestaurantID { get; set; }
        public string OrderStatus { get; set; }
        public decimal TotalPrice { get; set; }

        // Delivery details
        public string DeliveryAddress { get; set; }
        public string DeliveryCity { get; set; }
        public string DeliveryPostalCode { get; set; }

        public Users User { get; set; }
        public Restaurant Restaurant { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }

    public class OrderItem
    {
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int MenuItemID { get; set; }
        public int Quantity { get; set; }
        public decimal ItemPrice { get; set; }
        public string Customization { get; set; }
        public Order Order { get; set; }
        public MenuItem MenuItem { get; set; }
    }
}
