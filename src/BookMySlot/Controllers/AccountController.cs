using BookMySlot.Models;
using BookMySlot.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookMySlot.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDefaultDashboard();
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in successfully.", model.Email);
            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return await RedirectUserBasedOnRole(user);
        }

        ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> DemoAutoLogin(string role, string returnUrl)
    {
        string targetEmail = role switch
        {
            "Admin" => "admin@bookmyslot.com",
            "Provider" => "provider@bookmyslot.com",
            _ => "customer@bookmyslot.com"
        };

        var user = await _userManager.FindByEmailAsync(targetEmail);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return await RedirectUserBasedOnRole(user);
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickLogin(string role)
    {
        string targetEmail = role switch
        {
            "Admin" => "admin@bookmyslot.com",
            "Provider" => "provider@bookmyslot.com",
            _ => "customer@bookmyslot.com"
        };

        var user = await _userManager.FindByEmailAsync(targetEmail);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = $"Signed in via Quick Demo as {user.FullName} ({role}).";
            return await RedirectUserBasedOnRole(user);
        }

        TempData["ErrorMessage"] = "Demo account not found. Please log in manually.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDefaultDashboard();
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Public registration creates Customer accounts only
            await _userManager.AddToRoleAsync(user, "Customer");

            _logger.LogInformation("New Customer account registered for {Email}.", model.Email);
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["SuccessMessage"] = $"Welcome to BookMySlot, {user.FullName}! Your customer account has been created.";
            return RedirectToAction("Dashboard", "Customer");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        TempData["SuccessMessage"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<IActionResult> RedirectUserBasedOnRole(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin"))
        {
            return RedirectToAction("Dashboard", "Admin");
        }
        if (roles.Contains("Provider"))
        {
            return RedirectToAction("Dashboard", "Provider");
        }
        return RedirectToAction("Dashboard", "Customer");
    }

    private IActionResult RedirectToDefaultDashboard()
    {
        if (User.IsInRole("Admin"))
            return RedirectToAction("Dashboard", "Admin");
        if (User.IsInRole("Provider"))
            return RedirectToAction("Dashboard", "Provider");
        return RedirectToAction("Dashboard", "Customer");
    }
}
