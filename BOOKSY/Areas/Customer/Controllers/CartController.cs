using BOOKSY.DataAccess.Repository.IRepository;
using BOOKSY.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BOOKSY.Models;
using BOOKSY.Utility;
using Stripe.Checkout;
using Stripe;

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
            try
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
                if (appUser.CompanyId.GetValueOrDefault() == 0)
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
                if (appUser.CompanyId.GetValueOrDefault() == 0)
                {
                    var domain = "https://127.0.0.1:5223/";
                    var options = new SessionCreateOptions
                    {
                        SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
                        CancelUrl = domain + "customer/cart/index",
                        LineItems = new List<SessionLineItemOptions>(),
                        Mode = "payment",
                        PaymentMethodTypes = new List<string> { "card" },
                    };
                    foreach (var item in shoppingCartVM.ShoppingCartList)
                    {
                        SessionLineItemOptions sessionLineItemOptions = new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(item.Price * 100),
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = item.Product.Title,
                                },
                            },
                            Quantity = item.Count,
                        };
                        options.LineItems.Add(sessionLineItemOptions);
                    }

                    var service = new SessionService();
                    Session session = service.Create(options);
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                    _unitOfWork.Save();

                    return Redirect(session.Url);


                }
                return RedirectToAction("OrderConfirmation", new { id = shoppingCartVM.OrderHeader.Id });
            }
            catch (StripeException stripeEx)
            {
                System.Diagnostics.Debug.WriteLine($"Stripe Error: {stripeEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Stripe Error Code: {stripeEx.StripeError?.Code}");
                System.Diagnostics.Debug.WriteLine($"Stripe Error Type: {stripeEx.StripeError?.Type}");
                TempData["Error"] = $"Payment error: {stripeEx.Message}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["Error"] = "An error occurred during checkout. Please try again.";
                return RedirectToAction("Index");
            }
        }
        
        public IActionResult OrderConfirmation(int id)
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == id, IncludeProperties: "AppUser");
            if(orderHeaderFromDb.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                SessionService service = new SessionService();
                Session session = service.Get(orderHeaderFromDb.SessionId);
                if(session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(id, orderHeaderFromDb.SessionId, orderHeaderFromDb.PaymentIntentId);
                    _unitOfWork.Save();
                }
            }
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(u => u.AppUserId == orderHeaderFromDb.AppUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
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
