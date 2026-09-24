using web_app_scratch.Attributes;
using web_app_scratch.Models;

namespace web_app_scratch.Controllers;

public class ProductController
{
    [HttpGet("/products")]
    public string GetAll()
    {
        return "All products: [Laptop, Phone]";
    }

    [HttpPost("/products")]
    public string Create(Product product)
    {
        return $"Created product: {product.Name} (${product.Price})";
    }
}