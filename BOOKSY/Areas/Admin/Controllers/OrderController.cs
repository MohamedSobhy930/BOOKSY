using BOOKSY.DataAccess.Repository.IRepository;
using BOOKSY.Models;
using BOOKSY.Models.ViewModels;
using BOOKSY.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BOOKSY.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int orderId)
        {
            OrderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, IncludeProperties: "AppUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.Id == orderId, IncludeProperties: "Product")
            };
            return View(OrderVM);
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetails(OrderVM OrderVM)
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            if(OrderVM.OrderHeader.Carrier != null)
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if(OrderVM.OrderHeader.TrackingNumber != null)
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully";
            return RedirectToAction("Details", new { orderId = orderHeaderFromDb.Id });
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id,SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Status Updated Successfully";
            return RedirectToAction("Details", new { orderId = OrderVM.OrderHeader.Id });
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            List<OrderHeader> OrderHeaders ;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                OrderHeaders = _unitOfWork.OrderHeader.GetAll(IncludeProperties: "AppUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                OrderHeaders = _unitOfWork.OrderHeader
                    .GetAll(u => u.AppUserId == userId , IncludeProperties:"AppUser" ).ToList();
            }
            if (status == "pending")
            {
                OrderHeaders = OrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment).ToList();
            }
            else if(status == "inprocess")
            {
                OrderHeaders = OrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess).ToList();
            }
            else if (status == "completed")
            {
                OrderHeaders = OrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped).ToList();
            }
            else if (status == "approved")
            {
                OrderHeaders = OrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved).ToList();
            }
                return Json(new { data = OrderHeaders });
        }
        #endregion
    }
}
