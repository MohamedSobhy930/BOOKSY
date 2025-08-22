﻿using Microsoft.AspNetCore.Mvc;
using BOOKSY.Models;
using BOOKSY.DataAccess.Repository.IRepository;
using System.Security.Claims;
namespace BOOKSY.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                if(HttpContext.Session.GetInt32("SessionShoppingCart") == null)
                {
                    HttpContext.Session.SetInt32("SessionShoppingCart",
                _unitOfWork.ShoppingCart.GetAll(u => u.AppUserId == claim.Value).Count());
                }
                return View(HttpContext.Session.GetInt32("SessionShoppingCart"));
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}
