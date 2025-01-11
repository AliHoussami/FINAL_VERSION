namespace projet_info_finale.Models
{
    public class Restaurant
    {
        public int RestaurantID { get; set; }
        public string RestaurantName { get; set; }
        public string Description { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string OpeningHours { get; set; }
        public decimal? Rating { get; set; }
        public ICollection<MenuItem> MenuItems { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}
