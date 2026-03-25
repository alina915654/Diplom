namespace Diplom.ViewModels
{
public class CheckoutViewModel
    {
        public int PaymentMethod { get; set; }
        public int TypeId { get; set; }
        public List<CartItemViewModel> Items { get; set; } = [];
    }

    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}