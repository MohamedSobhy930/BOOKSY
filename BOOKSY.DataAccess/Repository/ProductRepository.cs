using BOOKSY.DataAccess.Data;
using BOOKSY.DataAccess.Repository.IRepository;
using BOOKSY.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOOKSY.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly AppDbContext _appDbContext;
        public ProductRepository(AppDbContext appDbContext) : base(appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public void Update(Product product)
        {
            Product productFromDB = _appDbContext.Products.FirstOrDefault(p => p.Id == product.Id);
            if (productFromDB != null)
            {
                productFromDB.Title = product.Title;
                productFromDB.ISBN = product.ISBN;
                productFromDB.Description = product.Description;
                productFromDB.Author = product.Author;
                productFromDB.ListPrice = product.ListPrice;
                productFromDB.Price = product.Price;
                productFromDB.Price50 = product.Price50;
                productFromDB.Price100 = product.Price100;
                productFromDB.CategoryId = product.CategoryId;

                if (product.ImageUrl != null)
                {
                    productFromDB.ImageUrl = product.ImageUrl;
                }

            }
        }
    }
}
