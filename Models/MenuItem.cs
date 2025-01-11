namespace projet_info_finale.Models
{
    public class MenuItem
    {
        public int MenuItemID { get; set; }
        public int RestaurantID { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool Availability { get; set; }
        public ICollection<Ingredient> Ingredients { get; set; }
        public Restaurant Restaurant { get; set; }
    }
}

