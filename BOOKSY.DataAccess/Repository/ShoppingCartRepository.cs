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
    public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
    {
        private readonly AppDbContext _appDbContext;
        public ShoppingCartRepository(AppDbContext appDbContext) : base(appDbContext) 
        {
            _appDbContext = appDbContext; 
        }
        public void Update(ShoppingCart shoppingCart)
        {
            _appDbContext.ShoppingCarts.Update(shoppingCart);
        }
    }
}
