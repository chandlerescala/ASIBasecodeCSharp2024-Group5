﻿using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.WebApp.Authentication;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.WebApp.Controllers
{
    public class AccountController : BaseController
    {
        private readonly SessionManager _sessionManager;
        private readonly SignInManager _signInManager;
        private readonly TokenValidationParametersFactory _tokenValidationParametersFactory;
        private readonly TokenProviderOptionsFactory _tokenProviderOptionsFactory;
        private readonly IConfiguration _appConfiguration;
        //private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountController"/> class.
        /// </summary>
        /// <param name="signInManager">The sign in manager.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="tokenValidationParametersFactory">The token validation parameters factory.</param>
        /// <param name="tokenProviderOptionsFactory">The token provider options factory.</param>
        public AccountController(
                            SignInManager signInManager,
                            IHttpContextAccessor httpContextAccessor,
                            ILoggerFactory loggerFactory,
                            IConfiguration configuration,
                            IMapper mapper,
                            //IUserService userService,
                            TokenValidationParametersFactory tokenValidationParametersFactory,
                            TokenProviderOptionsFactory tokenProviderOptionsFactory) : base (httpContextAccessor)
        {
            this._sessionManager = new SessionManager(this._session);
            this._signInManager = signInManager;
            this._tokenProviderOptionsFactory = tokenProviderOptionsFactory;
            this._tokenValidationParametersFactory = tokenValidationParametersFactory;
            this._appConfiguration = configuration;
            //this._userService = userService;
        }

        /// <summary>
        /// Login Method
        /// </summary>
        /// <returns>Created response view</returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            TempData["returnUrl"] = System.Net.WebUtility.UrlDecode(HttpContext.Request.Query["ReturnUrl"]);
            this._sessionManager.Clear();
            this._session.SetString("SessionId", System.Guid.NewGuid().ToString());

            if (User.Identity.IsAuthenticated)
            {
                switch (User.FindFirst("UserRole")?.Value)
                {
                    case "superadmin":
                        return RedirectToAction("Index", "SuperAdminDashboard");
                    case "administrator":
                        return RedirectToAction("Index", "AdminDashboard");
                    case "user":
                        return RedirectToAction("Index", "UserDashboard");
                    case "support agent":
                        return RedirectToAction("Index", "AgentDashboard");
                    default:
                        return View();
                }
            }
            return View();
        }

        /// <summary>
        /// Authenticate user and signs the user in when successful.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns> Created response view </returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(User u, string rememberMe)
        {
            this._session.SetString("HasSession", "Exist");

            var user = _db.VwUserRoleViews.Where(m => m.Email == u.Email && m.Password == u.Password).FirstOrDefault();

            if (user == null)
            {
                TempData["ErrorMsg"] = "Invalid credentials, please try again";
                return View();
            }

            // 認証OK
            var isPersistent = !string.IsNullOrEmpty(rememberMe) && rememberMe.ToLower() == "on" ? true : false;
            await _signInManager.SignInAsync(user, isPersistent);
            this._session.SetString("UserName", user.Name);

            switch (user.RoleName)
            {
                case "superadmin":
                    return RedirectToAction("Index", "SuperAdminDashboard");
                case "administrator":
                    return RedirectToAction("Index", "AdminDashboard");
                case "user":
                    return RedirectToAction("Index", "UserDashboard");
                case "support agent":
                    return RedirectToAction("Index", "AgentDashboard");
                default:
                    // 認証NG
                    TempData["ErrorMsg"] = "An error has occured, please try again";
                    return View();
            }
        }

        //[HttpGet]
        //[AllowAnonymous]
        //public IActionResult Register()
        //{
        //    return View();
        //}

        //[HttpPost]
        //[AllowAnonymous]
        //public IActionResult Register(User model)
        //{
        //    try
        //    {
        //        //_userService.AddUser(model);
        //        return RedirectToAction("Login", "Account");
        //    }
        //    catch(InvalidDataException ex)
        //    {
        //        TempData["ErrorMessage"] = ex.Message;
        //    }
        //    catch(Exception)
        //    {
        //        TempData["ErrorMessage"] = Resources.Messages.Errors.ServerError;
        //    }
        //    return View();
        //}

        /// <summary>
        /// Sign Out current account and return login view.
        /// </summary>
        /// <returns>Created response view</returns>
        [AllowAnonymous]
        public async Task<IActionResult> SignOutUser()
        {
            await this._signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}
