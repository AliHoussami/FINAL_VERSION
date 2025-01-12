namespace projet_info_finale.Models
{
    public class CartItem
    {
        public int CartItemID { get; set; }
        public int MenuItemID { get; set; }
        public string? Customizations { get; set; } // Additional ingredients or notes
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public int UserID { get; set; }

        // Navigation properties
        public MenuItem MenuItem { get; set; }
    }
}
