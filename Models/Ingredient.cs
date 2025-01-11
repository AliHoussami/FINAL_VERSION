namespace projet_info_finale.Models
{
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MenuItemID { get; set; }
        public MenuItem MenuItem { get; set; }
    }
}
