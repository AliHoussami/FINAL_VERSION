namespace projet_info_finale.Models
{
    public class Delivery
    {
        public int DeliveryID { get; set; }
        public int OrderID { get; set; }
        public int DriverID { get; set; }
        public string DeliveryStatus { get; set; } // Pending, Delivering, Delivered
        public Order Order { get; set; }
    }
}
