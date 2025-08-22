using BOOKSY.DataAccess.Data;
using BOOKSY.DataAccess.Repository.IRepository;
using BOOKSY.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BOOKSY.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category> , ICategoryRepository 
    {
       private readonly AppDbContext _appDbContext;
        public CategoryRepository(AppDbContext appDbContext) : base(appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public void Update(Category category)
        {
            _appDbContext.Update(category);
        }
    }
}
