using ITRepairService.Models;
using ITRepairService.Services;
using ITRepairService.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ITRepairService.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILdapAuthenticationService ldapAuthService,
    ILogger<AccountController> logger,
    IDepartmentInitializerService departmentInitializerService) : Controller
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly ILdapAuthenticationService _ldapAuthService = ldapAuthService;
    private readonly ILogger<AccountController> _logger = logger;
    private readonly IDepartmentInitializerService _departmentInitializerService = departmentInitializerService;

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 1. Try AD/LDAP authentication first
        LdapAuthenticationResult? ldapResult = null;
        try
        {
            ldapResult = _ldapAuthService.Authenticate(model.UserName, model.Password);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LDAP authentication threw exception for user: {Username}, falling back to local authentication", model.UserName);
        }
        
        // If LDAP authentication succeeds, proceed with AD user flow
        if (ldapResult is not null && ldapResult.IsSuccess)
        {
            _logger.LogInformation("LDAP authentication successful for user: {Username}", model.UserName);
            _logger.LogInformation("LDAP authentication successful for user: {Username}, DisplayName: {DisplayName}, Title: {Title}, Dept: {Dept}", 
                ldapResult.Username, ldapResult.DisplayName, ldapResult.Title, ldapResult.Department);
            
            // Store AD data in ViewBag for display
            ViewBag.AdData = new
            {
                Username = ldapResult.Username,
                DisplayName = ldapResult.DisplayName,
                Email = ldapResult.Email,
                Department = ldapResult.Department,
                Title = ldapResult.Title,
                Company = ldapResult.Company,
                Manager = ldapResult.Manager,
                TelephoneNumber = ldapResult.TelephoneNumber,
                EmployeeID = ldapResult.EmployeeID
            };
            
            // Check if user exists locally; if not, auto-create
            var user = await _userManager.FindByNameAsync(ldapResult.Username);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = ldapResult.Username,
                    Email = ldapResult.Email,
                    FullName = ldapResult.DisplayName,
                    Department = ldapResult.Department,
                    Title = ldapResult.Title,
                    Company = ldapResult.Company,
                    Manager = ldapResult.Manager,
                    TelephoneNumber = ldapResult.TelephoneNumber,
                    EmployeeID = ldapResult.EmployeeID,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedByName = ldapResult.DisplayName,
                    UpdatedByName = ldapResult.DisplayName,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Create with a random password (user will authenticate via AD)
                var createResult = await _userManager.CreateAsync(user, Guid.NewGuid().ToString("N") + "Aa1!");
                if (createResult.Succeeded)
                {
                    _logger.LogInformation("User created successfully: {Username}", ldapResult.Username);
                    
                    // Determine if user is a manager (SM/DM)
                    var isManager = !string.IsNullOrWhiteSpace(ldapResult.Title) &&
                        (ldapResult.Title.ToUpperInvariant().Contains("SM") || 
                         ldapResult.Title.ToUpperInvariant().Contains("SECTION MANAGER") ||
                         ldapResult.Title.ToUpperInvariant().Contains("DM") || 
                         ldapResult.Title.ToUpperInvariant().Contains("DEPARTMENT MANAGER"));
                    
                    // Determine if user is in DX Department
                    var isDXDepartment = !string.IsNullOrWhiteSpace(ldapResult.Department) &&
                        ldapResult.Department.ToUpperInvariant().Contains("DX");
                    
                    // Assign roles based on conditions
                    // All users get User role
                    var userRoleResult = await _userManager.AddToRoleAsync(user, AppRoles.User);
                    if (userRoleResult.Succeeded)
                    {
                        _logger.LogInformation("✓ Assigned User role to: {Username}", ldapResult.Username);
                    }
                    else
                    {
                        _logger.LogError("Failed to assign User role to {Username}: {Errors}", 
                            ldapResult.Username, string.Join(", ", userRoleResult.Errors.Select(e => e.Description)));
                    }
                    
                    if (isManager)
                    {
                        // Manager (SM/DM): Assign Approve
                        var approveResult = await _userManager.AddToRoleAsync(user, AppRoles.Approve);
                        if (approveResult.Succeeded)
                        {
                            _logger.LogInformation("✓ Granted Approve role to manager: {Username}, Title: {Title}", 
                                ldapResult.Username, ldapResult.Title);
                        }
                        else
                        {
                            _logger.LogError("Failed to grant Approve role to {Username}: {Errors}", 
                                ldapResult.Username, string.Join(", ", approveResult.Errors.Select(e => e.Description)));
                        }
                        
                        // If DX Department: Also assign ITSupport + Admin
                        if (isDXDepartment)
                        {
                            var itSupportResult = await _userManager.AddToRoleAsync(user, AppRoles.ITSupport);
                            if (itSupportResult.Succeeded)
                            {
                                _logger.LogInformation("✓ Granted ITSupport role to DX manager: {Username}, Dept: {Dept}", 
                                    ldapResult.Username, ldapResult.Department);
                            }
                            else
                            {
                                _logger.LogError("Failed to grant ITSupport role to {Username}: {Errors}", 
                                    ldapResult.Username, string.Join(", ", itSupportResult.Errors.Select(e => e.Description)));
                            }
                            
                            var adminResult = await _userManager.AddToRoleAsync(user, AppRoles.Admin);
                            if (adminResult.Succeeded)
                            {
                                _logger.LogInformation("✓ Granted Admin role to DX manager: {Username}, Dept: {Dept}", 
                                    ldapResult.Username, ldapResult.Department);
                            }
                            else
                            {
                                _logger.LogError("Failed to grant Admin role to {Username}: {Errors}", 
                                    ldapResult.Username, string.Join(", ", adminResult.Errors.Select(e => e.Description)));
                            }
                        }
                    }
                    else if (isDXDepartment)
                    {
                        // Non-manager DX Department: Assign ITSupport + Approve
                        var itSupportResult = await _userManager.AddToRoleAsync(user, AppRoles.ITSupport);
                        if (itSupportResult.Succeeded)
                        {
                            _logger.LogInformation("✓ Granted ITSupport role to DX user: {Username}, Dept: {Dept}", 
                                ldapResult.Username, ldapResult.Department);
                        }
                        else
                        {
                            _logger.LogError("Failed to grant ITSupport role to {Username}: {Errors}", 
                                ldapResult.Username, string.Join(", ", itSupportResult.Errors.Select(e => e.Description)));
                        }
                        
                        var approveResult = await _userManager.AddToRoleAsync(user, AppRoles.Approve);
                        if (approveResult.Succeeded)
                        {
                            _logger.LogInformation("✓ Granted Approve role to DX user: {Username}, Dept: {Dept}", 
                                ldapResult.Username, ldapResult.Department);
                        }
                        else
                        {
                            _logger.LogError("Failed to grant Approve role to {Username}: {Errors}", 
                                ldapResult.Username, string.Join(", ", approveResult.Errors.Select(e => e.Description)));
                        }
                    }
                    
                    // Special case: p_mallika always gets Admin
                    if (ldapResult.Username.Equals("p_mallika", StringComparison.OrdinalIgnoreCase))
                    {
                        var adminResult = await _userManager.AddToRoleAsync(user, AppRoles.Admin);
                        if (adminResult.Succeeded)
                        {
                            _logger.LogInformation("✓ Granted Admin role to user: {Username}", ldapResult.Username);
                        }
                        else
                        {
                            _logger.LogError("Failed to grant Admin role to {Username}: {Errors}", 
                                ldapResult.Username, string.Join(", ", adminResult.Errors.Select(e => e.Description)));
                        }
                    }
                }
                else
                {
                    _logger.LogError("Failed to create user: {Username}, Errors: {Errors}", 
                        ldapResult.Username, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }

            if (user is not null)
            {
                // Determine if user is a manager (SM/DM)
                var isManager = !string.IsNullOrWhiteSpace(ldapResult.Title) &&
                    (ldapResult.Title.ToUpperInvariant().Contains("SM") || 
                     ldapResult.Title.ToUpperInvariant().Contains("SECTION MANAGER") ||
                     ldapResult.Title.ToUpperInvariant().Contains("DM") || 
                     ldapResult.Title.ToUpperInvariant().Contains("DEPARTMENT MANAGER"));
                
                // Determine if user is in DX Department
                var isDXDepartment = !string.IsNullOrWhiteSpace(ldapResult.Department) &&
                    ldapResult.Department.ToUpperInvariant().Contains("DX");
                
                // Grant Admin role to p_mallika if not already assigned
                if (ldapResult.Username.Equals("p_mallika", StringComparison.OrdinalIgnoreCase))
                {
                    var isAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
                    if (!isAdmin)
                    {
                        await _userManager.AddToRoleAsync(user, AppRoles.Admin);
                        _logger.LogInformation("Granted Admin role to existing user: {Username}", ldapResult.Username);
                    }
                }
                
                // Assign roles based on conditions for existing users
                // Condition 1: Title = SM/DM AND Dept = DX → ITSupport, User, Approve, Admin
                // Condition 2: Title = SM/DM AND Dept ≠ DX → User, Approve
                // Condition 3: Title = empty AND Dept ≠ DX → User
                // Condition 4: Title = empty AND Dept = DX → ITSupport, User, Approve
                
                // Always ensure User role is assigned
                var hasUserRole = await _userManager.IsInRoleAsync(user, AppRoles.User);
                if (!hasUserRole)
                {
                    await _userManager.AddToRoleAsync(user, AppRoles.User);
                    _logger.LogInformation("✓ Assigned User role to existing user: {Username}", ldapResult.Username);
                }
                
                if (isManager)
                {
                    // Manager (SM/DM): Grant Approve role
                    var hasApproveRole = await _userManager.IsInRoleAsync(user, AppRoles.Approve);
                    if (!hasApproveRole)
                    {
                        await _userManager.AddToRoleAsync(user, AppRoles.Approve);
                        _logger.LogInformation("✓ Granted Approve role to existing manager: {Username}, Title: {Title}", 
                            ldapResult.Username, ldapResult.Title);
                    }
                    
                    if (isDXDepartment)
                    {
                        // DX Department Manager: ITSupport + Admin
                        var hasITSupportRole = await _userManager.IsInRoleAsync(user, AppRoles.ITSupport);
                        if (!hasITSupportRole)
                        {
                            await _userManager.AddToRoleAsync(user, AppRoles.ITSupport);
                            _logger.LogInformation("✓ Granted ITSupport role to existing DX manager: {Username}, Dept: {Dept}", 
                                ldapResult.Username, ldapResult.Department);
                        }
                        
                        var hasAdminRole = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
                        if (!hasAdminRole)
                        {
                            await _userManager.AddToRoleAsync(user, AppRoles.Admin);
                            _logger.LogInformation("✓ Granted Admin role to existing DX manager: {Username}, Dept: {Dept}", 
                                ldapResult.Username, ldapResult.Department);
                        }
                    }
                }
                else if (isDXDepartment)
                {
                    // Non-manager DX Department: ITSupport + Approve
                    var hasITSupportRole = await _userManager.IsInRoleAsync(user, AppRoles.ITSupport);
                    if (!hasITSupportRole)
                    {
                        await _userManager.AddToRoleAsync(user, AppRoles.ITSupport);
                        _logger.LogInformation("✓ Granted ITSupport role to existing DX user: {Username}, Dept: {Dept}", 
                            ldapResult.Username, ldapResult.Department);
                    }
                    
                    var hasApproveRole = await _userManager.IsInRoleAsync(user, AppRoles.Approve);
                    if (!hasApproveRole)
                    {
                        await _userManager.AddToRoleAsync(user, AppRoles.Approve);
                        _logger.LogInformation("✓ Granted Approve role to existing DX user: {Username}, Dept: {Dept}", 
                            ldapResult.Username, ldapResult.Department);
                    }
                }
                else
                {
                    // Non-DX Department, Non-manager: Only User role (already assigned above)
                    _logger.LogInformation("✓ Existing user {Username} is non-manager and non-DX department - User role only", 
                        ldapResult.Username);
                }
                
                // Bypass password check and sign in using the external/AD identity
                await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

                // Log AD data after successful login
                _logger.LogInformation("========================================");
                _logger.LogInformation("AD LOGIN SUCCESSFUL - AD DATA RETRIEVED:");
                _logger.LogInformation("========================================");
                _logger.LogInformation("Username:        {Username}", ldapResult.Username);
                _logger.LogInformation("DisplayName:     {DisplayName}", ldapResult.DisplayName);
                _logger.LogInformation("Email:           {Email}", ldapResult.Email);
                _logger.LogInformation("Department:      {Department}", ldapResult.Department);
                _logger.LogInformation("Title:           {Title}", ldapResult.Title);
                _logger.LogInformation("Company:         {Company}", ldapResult.Company);
                _logger.LogInformation("Manager:         {Manager}", ldapResult.Manager);
                _logger.LogInformation("Telephone:       {Telephone}", ldapResult.TelephoneNumber);
                _logger.LogInformation("EmployeeID:      {EmployeeID}", ldapResult.EmployeeID);
                _logger.LogInformation("MemberOf Count:  {MemberCount}", ldapResult.MemberOf?.Count ?? 0);
                _logger.LogInformation("----------------------------------------");
                _logger.LogInformation("Role Assignment Logic:");
                _logger.LogInformation("  Is Manager (SM/DM): {IsManager}", isManager);
                _logger.LogInformation("  Is DX Department:   {IsDXDepartment}", isDXDepartment);
                _logger.LogInformation("Roles Assigned:");
                _logger.LogInformation("  - User:        Always assigned");
                if (isManager)
                {
                    _logger.LogInformation("  - Approve:     Manager (SM/DM)");
                    if (isDXDepartment)
                    {
                        _logger.LogInformation("  - ITSupport:   DX Department Manager");
                        _logger.LogInformation("  - Admin:       DX Department Manager");
                    }
                }
                else if (isDXDepartment)
                {
                    _logger.LogInformation("  - ITSupport:   DX Department");
                    _logger.LogInformation("  - Approve:     DX Department");
                }
                if (ldapResult.Username.Equals("p_mallika", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("  - Admin:       Special case (p_mallika)");
                }
                _logger.LogInformation("========================================");

                // Ensure department exists in master data
                if (!string.IsNullOrWhiteSpace(ldapResult.Department))
                {
                    try
                    {
                        var departmentAdded = await _departmentInitializerService.EnsureDepartmentExistsAsync(ldapResult.Department);
                        if (departmentAdded)
                        {
                            _logger.LogInformation("✓ Auto-created new department in master data: {Department}", ldapResult.Department);
                        }
                        else
                        {
                            _logger.LogInformation("✓ Department already exists in master data: {Department}", ldapResult.Department);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to ensure department exists: {Department}", ldapResult.Department);
                    }
                }

                if (user.MustChangePassword)
                {
                    return RedirectToAction("ChangePassword", "Account");
                }

                return RedirectToLocal(model.ReturnUrl);
            }
        }

        // 2. Fallback: try local Identity authentication (for locally registered users)
        _logger.LogInformation("LDAP authentication failed or not available for user: {Username}, trying local authentication", model.UserName);
        var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var localUser = await _userManager.FindByNameAsync(model.UserName);
            if (localUser is not null && localUser.MustChangePassword)
            {
                return RedirectToAction("ChangePassword", "Account");
            }
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true,
            CreatedByName = model.FullName,
            UpdatedByName = model.FullName,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (createResult.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, AppRoles.User);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToLocal(model.ReturnUrl);
        }

        foreach (var error in createResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        var changeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!changeResult.Succeeded)
        {
            foreach (var error in changeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        await _signInManager.RefreshSignInAsync(user);

        TempData["SuccessMessage"] = "เปลี่ยนรหัสผ่านเรียบร้อยแล้ว";
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}