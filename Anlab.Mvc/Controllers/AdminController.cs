using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anlab.Core.Data;
using Anlab.Core.Domain;
using AnlabMvc.Models.Order;
using AnlabMvc.Models.Roles;
using AnlabMvc.Models.User;
using AnlabMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnlabMvc.Controllers
{
    [Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.LabUser)]
    public class AdminController : ApplicationController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AdminController(ApplicationDbContext dbContext, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = RoleCodes.Admin)]
        public async Task<IActionResult> Index()
        {
            // TODO: find better way than super select
            var users = await _userManager.GetUsersInRoleAsync(RoleCodes.Admin);
            users = users.Union(await _userManager.GetUsersInRoleAsync(RoleCodes.LabUser)).ToList();
            users = users.Union(await _userManager.GetUsersInRoleAsync(RoleCodes.Reports)).ToList();

            var usersInRoles = users.Select(u => new UserRolesModel { User = u }).ToList();

            foreach (var userRole in usersInRoles)
            {

                userRole.IsAdmin = await _userManager.IsInRoleAsync(userRole.User, RoleCodes.Admin);
                userRole.IsLabUser = await _userManager.IsInRoleAsync(userRole.User, RoleCodes.LabUser);
                userRole.IsReports = await _userManager.IsInRoleAsync(userRole.User, RoleCodes.Reports);
            }

            return View(usersInRoles);
        }
        [Authorize(Roles = RoleCodes.Admin)]
        [HttpPost]
        public async Task<IActionResult> AddAminUser(string id)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(a => a.NormalizedUserName == id.ToUpper().Trim());
            if (user == null)
            {
                ErrorMessage = $"Email {id} not found";
                return RedirectToAction("Index");
            }

            return RedirectToAction("EditAdmin", new {id = user.Id});
        }

        [Authorize(Roles = RoleCodes.Admin)]
        [HttpGet]
        public async Task<IActionResult> EditAdmin(string id)
        {
            var model = new UserRolesModel();
            model.User = _dbContext.Users.SingleOrDefault(a => a.Id == id);
            if (model.User == null)
            {
                return NotFound();
            }
            model.IsAdmin = await _userManager.IsInRoleAsync(model.User, RoleCodes.Admin);
            model.IsLabUser = await _userManager.IsInRoleAsync(model.User, RoleCodes.LabUser);
            model.IsReports = await _userManager.IsInRoleAsync(model.User, RoleCodes.Reports);

            return View(model);
        }

        public async Task<IActionResult> ListClients()
        {
            // TODO: filter out admin and lab users
            var users = await _dbContext.Users.AsNoTracking().ToListAsync();

            return View(users);
        }

        public IActionResult EditUser(string id)
        {
            var user = _dbContext.Users.SingleOrDefault(a => a.Id == id);
            if (user == null)
            {
                ErrorMessage = "User Not Found";
                return RedirectToAction("ListClients");
            }

            return View(user);
        }

        [HttpPost]
        public IActionResult EditUser(string id, User user)
        {
            var userToUpdate = _dbContext.Users.SingleOrDefault(a => a.Id == id);
            if (userToUpdate == null)
            {
                ErrorMessage = "User Not Found";
                return RedirectToAction("ListClients");
            }
            if (ModelState.IsValid)
            {
                userToUpdate.FirstName = user.FirstName;
                userToUpdate.LastName = user.LastName;
                userToUpdate.Name = user.Name;
                userToUpdate.Phone = user.Phone;
                userToUpdate.Account = user.Account?.ToUpper();
                userToUpdate.ClientId = user.ClientId?.ToUpper();
                userToUpdate.CompanyName = user.CompanyName;
                userToUpdate.BillingContactName = user.BillingContactName;
                userToUpdate.BillingContactAddress = user.BillingContactAddress;
                userToUpdate.BillingContactEmail = user.BillingContactEmail;
                userToUpdate.BillingContactPhone = user.BillingContactPhone;

                _dbContext.Update(userToUpdate);
                _dbContext.SaveChanges();

                return RedirectToAction("ListClients");
            }

            return View(user);
        }

        [Authorize(Roles = RoleCodes.Admin)]
        [HttpPost]
        public async Task<IActionResult> AddUserToRole(string userId, string role, bool add)
        {
            var user = _dbContext.Users.Single(a => a.Id == userId);
            if (add)
            {
                await _userManager.AddToRoleAsync(user, role);
            }
            else
            {
                if (user.Id == CurrentUserId)
                {
                    ErrorMessage = "Can't remove your own permissions.";
                    return RedirectToAction("Index");
                }
                await _userManager.RemoveFromRoleAsync(user, role);
            }

            return RedirectToAction("EditAdmin", new {id=user.Id});
        }

        [Authorize(Roles =  RoleCodes.Admin)]
        public async Task<IActionResult> CreateRole(string role) //TODO: Remove after role created
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
            return Content($"Added {role} Role");
        }

        public async Task<IActionResult> MailQueue(int? id = null)
        {
            // Right now, show unsent pending emails, failures, and successfully sent within 30 days.
            // TODO: Review filter

            List<MailMessage> messages = null;
            if (id.HasValue)
            {
                messages = await _dbContext.MailMessages.Include(i => i.Order).Where(x => x.Order.Id == id).AsNoTracking().ToListAsync();
            }
            else
            {
                messages = await _dbContext.MailMessages.Include(i => i.Order).Where(x =>
                    x.Sent == null || !x.Sent.Value || x.Sent.Value && x.SentAt != null &&
                    x.SentAt.Value >= DateTime.UtcNow.AddDays(-30)).AsNoTracking().ToListAsync();
            }
            return View(messages);
        }

        public IActionResult ViewMessage(int id)
        {

            var message = _dbContext.MailMessages.AsNoTracking().SingleOrDefault(x => x.Id == id);
            return View(message);
        }
    }
}
