using Microsoft.EntityFrameworkCore;
using Provider.Database;

namespace Provider.Api;

public interface IProductService
{
    Task<List<Product>> GetAllProducts();
    Task<List<Product>> GetProductsByName(string name);
    Task<Product?> GetProduct(int productId);
}

public class ProductService : IProductService
{
    private readonly ProductContext _context;

    public ProductService(ProductContext context)
    {
        _context = context;
    }
    
    public async Task<List<Product>> GetAllProducts()
    {
        return await _context.Products.ToListAsync();
    }
    
    public async Task<List<Product>> GetProductsByName(string name)
    {
        return await _context.Products.Where(x => x.Name == name).ToListAsync();
    }
    
    public async Task<Product?> GetProduct(int productId)
    {
        return await _context.Products.FirstOrDefaultAsync(x => x.Id == productId);
    }
}