using BOOKSY.DataAccess.Repository.IRepository;
using BOOKSY.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BOOKSY.Models;
using BOOKSY.Utility;

namespace BOOKSY.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM shoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            shoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart
                .GetAll(u => u.AppUserId == userId, IncludeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };
            foreach(var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.Price = PriceBasedOnQuantity(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(shoppingCartVM);
        }
        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count += 1;
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if (cartFromDb.Count == 1)
            {
                HttpContext.Session.SetInt32("SessionShoppingCart",
                    _unitOfWork.ShoppingCart.GetAll(u => u.AppUserId == cartFromDb.AppUserId).Count()-1);
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                _unitOfWork.Save();
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.Save();
            }
            return RedirectToAction("Index");
        }
        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            HttpContext.Session.SetInt32("SessionShoppingCart",
                    _unitOfWork.ShoppingCart.GetAll(u => u.AppUserId == cartFromDb.AppUserId).Count()-1);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult CheckOut(int cartId)
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            shoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart
                .GetAll(u => u.AppUserId == userId, IncludeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };
            shoppingCartVM.OrderHeader.AppUser = _unitOfWork.AppUser.Get(u => u.Id == userId);

            shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.AppUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.AppUser.PhoneNumber;
            shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.AppUser.StreetAddress;
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.AppUser.City;
            shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.AppUser.State;


            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.Price = PriceBasedOnQuantity(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(shoppingCartVM);
        }
        [HttpPost]
        [ActionName("CheckOut")]
        public IActionResult CheckOutPost()
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            shoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart
                .GetAll(u => u.AppUserId == userId, IncludeProperties: "Product");
            shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            shoppingCartVM.OrderHeader.AppUserId = userId;


            AppUser appUser = _unitOfWork.AppUser.Get(u => u.Id == userId);
            

            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.Price = PriceBasedOnQuantity(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            if(appUser.CompanyId.GetValueOrDefault() == 0)
            {
                // user is not a company user
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            }
            else
            {
                // user is a company user
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;

            }

            if(appUser.CompanyId.GetValueOrDefault() == 0)
            {
                
            }
            
            _unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = shoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }
            return RedirectToAction("OrderConfirmation", new { id = shoppingCartVM.OrderHeader});
        }
        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }
        private double PriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if(shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }
            else
            {
                return shoppingCart.Product.Price100;
            }
        }
    }
}
