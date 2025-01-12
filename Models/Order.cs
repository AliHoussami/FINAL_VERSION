namespace projet_info_finale.Models
{
    public enum OrderStatus
    {
        Pending,
        Preparing,
        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "Out for Delivery")]
        OutForDelivery,
        Completed,
        Cancelled
    }

    public class Order
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public int RestaurantID { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending; // Set default value
        public decimal TotalPrice { get; set; }
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
