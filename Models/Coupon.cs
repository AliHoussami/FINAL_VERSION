namespace projet_info_finale.Models
{
    public class Coupon
    {
        public int CouponID { get; set; }
        public string Code { get; set; }
        public decimal Value { get; set; }
        public string DiscountType { get; set; } // Amount, Percentage
        public bool IsActive { get; set; }
    }
}
