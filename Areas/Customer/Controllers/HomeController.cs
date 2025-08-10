using System.Diagnostics;
using System.Security.Claims;
using BOOKSY.DataAccess.Repository.IRepository;
using BOOKSY.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOOKSY.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger,IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                HttpContext.Session.SetInt32("SessionShoppingCart",
                    _unitOfWork.ShoppingCart.GetAll(u => u.AppUserId == claim.Value).Count());
            }
            var productList = _unitOfWork.Product.GetAll(IncludeProperties:"category");
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            ShoppingCart cart = new()
            {
                Product = _unitOfWork.Product.Get(p => p.Id == id, IncludeProperties: "category"),
                Count = 1,
                ProductId = id

            };

            return View(cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            cart.AppUserId = userId;
            cart.Id = 0;
            var cartFromDb = _unitOfWork.ShoppingCart.Get(
                u => u.AppUserId == userId && u.ProductId == cart.ProductId);
            if (cartFromDb != null)
            {
                cartFromDb.Count += cart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
            }
            else
            {
                _unitOfWork.ShoppingCart.Add(cart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32("SessionShoppingCart",
                    _unitOfWork.ShoppingCart.GetAll(u => u.AppUserId == userId).Count());
            }

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
