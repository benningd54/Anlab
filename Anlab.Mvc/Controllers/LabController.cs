using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Anlab.Core.Data;
using Anlab.Core.Models;
using AnlabMvc.Models.FileUploadModels;
using AnlabMvc.Models.Order;
using AnlabMvc.Models.Roles;
using AnlabMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anlab.Core.Domain;

namespace AnlabMvc.Controllers
{
    [Authorize(Roles = RoleCodes.Admin + "," + RoleCodes.User)]
    public class LabController : ApplicationController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IOrderService _orderService;
        private readonly IOrderMessageService _orderMessageService;
        private readonly IFileStorageService _fileStorageService;

        private const int _maxShownOrders = 1000;

        public LabController(ApplicationDbContext dbContext, IOrderService orderService, IOrderMessageService orderMessageService, IFileStorageService fileStorageService)
        {
            _dbContext = dbContext;
            _orderService = orderService;
            _orderMessageService = orderMessageService;
            _fileStorageService = fileStorageService;
        }

        public IActionResult Orders()
        {
            var orders = _dbContext.Orders
                .Where(a => a.Status != OrderStatusCodes.Created)
                .Select(c => new Order
                {
                    Id = c.Id,
                    Creator = new User { Email = c.Creator.Email, ClientId = c.Creator.ClientId },
                    Created = c.Created,
                    Updated = c.Updated,
                    Project = c.Project,
                    Status = c.Status,
                    ShareIdentifier = c.ShareIdentifier
                })
                .Take(_maxShownOrders)
                .ToList();

            return View(orders);
        }


        public async Task<IActionResult> Details(int id)
        {
            var order = await _dbContext.Orders.Include(i => i.Creator).SingleOrDefaultAsync(o => o.Id == id && o.Status != OrderStatusCodes.Created);

            if (order == null)
            {
                return NotFound(id);
            }

            var model = new OrderReviewModel();
            model.Order = order;
            model.OrderDetails = order.GetOrderDetails();
            model.TestItems = _dbContext.TestItems
                .Where(a => model.OrderDetails.SelectedTests.Select(s => s.Id).Contains(a.Id)).ToList();
            model.HideLabDetails = false;

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var order = await _dbContext.Orders.SingleOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(id);
            }           
            if(order.Status == OrderStatusCodes.Received)
            {
                ErrorMessage = "You cannot edit an order marked as received";
                return RedirectToAction("Orders");
            }
            var joined = await _orderService.PopulateTestItemModel(true);

            var model = new OrderEditModel
            {
                TestItems = joined.ToArray(),
                Order = order
            };

            return View(model);
        }

        public IActionResult ListUsersOrders(string id)
        {
            var orders = _dbContext.Orders.Where(a => a.CreatorId == id && a.Status != OrderStatusCodes.Created).ToArray();
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> Save(OrderSaveModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = new List<string>();
                foreach (var result in ModelState.Values)
                {
                    foreach (var errs in result.Errors)
                    {
                        errors.Add(errs.ErrorMessage);
                    }
                }

                //TODO: A better way to return errors. Or maybe not, they shouldn't really ever happen.
                return Json(new { success = false, message = "There were problems with your order. Unable to save. Errors: " + string.Join("--", errors) });
            }

            var idForRedirection = 0;

            if (model.OrderId.HasValue)
            {
                var orderToUpdate = await _dbContext.Orders.SingleAsync(a => a.Id == model.OrderId.Value);
                if (orderToUpdate.Status != OrderStatusCodes.Confirmed)
                {
                    return Json(new {success = false, message = "You may only edit a Confirmed order."});
                }

                await _orderService.PopulateOrder(model, orderToUpdate);

                idForRedirection = model.OrderId.Value;
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                return Json(new { success = false, message = "Order Id not found." });
            }


            return Json(new { success = true, id = idForRedirection });
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _dbContext.Orders.Include(i => i.Creator).SingleOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(id);
            }

            var model = new OrderReviewModel();
            model.Order = order;
            model.OrderDetails = order.GetOrderDetails();
            model.TestItems = _dbContext.TestItems
                .Where(a => model.OrderDetails.SelectedTests.Select(s => s.Id).Contains(a.Id)).ToList();
            model.HideLabDetails = false;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Confirmation(int id, bool confirm)
        {
            var order = await _dbContext.Orders.Include(i => i.Creator).SingleOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(id);
            }

            if (order.Status != OrderStatusCodes.Confirmed)
            {
                ErrorMessage = "You can only receive a confirmed order";
                return RedirectToAction("Orders");
            }

            order.Status = OrderStatusCodes.Received;

            await _orderService.SendOrderToAnlab(order);

            await _orderMessageService.EnqueueReceivedMessage(order);

            await _dbContext.SaveChangesAsync();

            Message = "Order marked as received";
            return RedirectToAction("Orders");

        }

        public async Task<IActionResult> UpdateFromCompletedTests(int id)
        {
            var order = await _dbContext.Orders.Include(i => i.Creator).SingleOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(id);
            }

            var result = await _orderService.OverwiteOrderWithTestsCompleted(order); //TODO: Just testing
            if (result.WasError)
            {
                ErrorMessage = string.Format("Error. Unable to continue. The following codes were not found locally: {0}", string.Join(",", result.MissingCodes));
                return RedirectToAction("Orders");
            }
            var orderDetails = order.GetOrderDetails();
            orderDetails.SelectedTests = result.SelectedTests;
            orderDetails.Total = orderDetails.SelectedTests.Sum(x => x.Total);

            order.SaveDetails(orderDetails);

            var model = new OrderReviewModel();
            model.Order = order;
            model.OrderDetails = order.GetOrderDetails();
            model.TestItems = _dbContext.TestItems
                .Where(a => model.OrderDetails.SelectedTests.Select(s => s.Id).Contains(a.Id)).ToList();
            model.HideLabDetails = false;

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateFromCompletedTests(int id, UpdateFromCompletedTestsModel model)
        {
            var order = await _dbContext.Orders.Include(i => i.Creator).SingleOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(id);
            }

            if (order.Status != OrderStatusCodes.Received)
            {
                ErrorMessage = "You can only Complete a Received order";
                //return RedirectToAction("Orders");
            }

            if (model.UploadFile == null || model.UploadFile.Length <= 0)
            {
                ErrorMessage = "You need to upload the results at this time.";
                return RedirectToAction("UpdateFromCompletedTests");
            }

            order.Status = OrderStatusCodes.Complete;

            var result = await _orderService.OverwiteOrderWithTestsCompleted(order); //TODO: Just testing
            if (result.WasError)
            {
                ErrorMessage = string.Format("Error. Unable to continue. The following codes were not found locally: {0}", string.Join(",", result.MissingCodes));
                return RedirectToAction("Orders");
            }

            //File Upload
            order.ResultsFileIdentifier = await _fileStorageService.UploadFile(model.UploadFile);

            var orderDetails = order.GetOrderDetails();

            orderDetails.SelectedTests = result.SelectedTests;
            orderDetails.Total = orderDetails.SelectedTests.Sum(x => x.Total);

            orderDetails.LabComments = model.LabComments;
            orderDetails.AdjustmentAmount = model.GrandTotal - orderDetails.Total;

            order.SaveDetails(orderDetails);

            await _orderMessageService.EnqueueCompletedMessage(order);

            await _dbContext.SaveChangesAsync();

            Message = "Order marked as Complete";
            return RedirectToAction("Orders");

        }

        [Authorize(Roles = RoleCodes.Admin)]
        public async Task<ActionResult> JsonDetails(int id)
        {
            var order = await _dbContext.Orders.SingleOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(id);
            }

            return new JsonResult(order.GetOrderDetails());
        }

    }
}