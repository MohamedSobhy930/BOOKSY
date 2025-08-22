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
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly AppDbContext _appDbContext;
        public CompanyRepository(AppDbContext appDbContext) : base(appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public void Update(Company company)
        {
            _appDbContext.Companies.Update(company);
        }
    }
}
