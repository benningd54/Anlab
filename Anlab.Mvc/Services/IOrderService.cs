﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anlab.Core.Data;
using Anlab.Core.Domain;
using Anlab.Core.Models;
using AnlabMvc.Models.Order;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AnlabMvc.Services
{
    public interface IOrderService
    {
        Task PopulateOrder(OrderSaveModel model, Order orderToUpdate);
        void PopulateOrderWithLabDetails(OrderSaveModel model, Order orderToUpdate);
        Task SendOrderToAnlab(Order order);

        List<TestItemModel> PopulateTestItemModel();
    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITestItemPriceService _itemPriceService;

        public OrderService(ApplicationDbContext context, ITestItemPriceService itemPriceService)
        {
            _context = context;
            _itemPriceService = itemPriceService;
        }

        public List<TestItemModel> PopulateTestItemModel()
        {
            var prices = _itemPriceService.GetPrices();
            var items = _context.TestItems.AsNoTracking().ToList();

            var joined = (from i in items
                join p in prices on i.Code equals p.Code
                select new TestItemModel
                {
                    Analysis = i.Analysis,
                    Category = i.Category,
                    Code = i.Code,
                    ExternalCost = Math.Ceiling(p.Cost * 1.5m),
                    Group = i.Group,
                    Id = i.Id,
                    InternalCost = Math.Ceiling(p.Cost),
                    ExternalSetupCost = Math.Ceiling(p.SetupCost * 1.5m),
                    InternalSetupCost = Math.Ceiling(p.SetupCost)
                }).ToList();
            return joined;
        }

        private async Task<TestDetails[]> CalculateTestDetails(OrderDetails orderDetails)
        {
            // TODO: Do we really want to match on ID, or Code, or some combination?
            var selectedTestIds = orderDetails.SelectedTests.Select(t => t.Id);
            var tests = await _context.TestItems.Where(a => selectedTestIds.Contains(a.Id)).ToListAsync();

            var calcualtedTests = new List<TestDetails>();

            foreach (var test in orderDetails.SelectedTests)
            {
                var dbTest = tests.Single(t => t.Id == test.Id);

                var cost = orderDetails.Payment.IsInternalClient ? dbTest.InternalCost : dbTest.ExternalCost;
                var costAndQuantity = cost * orderDetails.Quantity;

                calcualtedTests.Add(new TestDetails
                {
                    Id = dbTest.Id,
                    Analysis = dbTest.Analysis,
                    Code = dbTest.Code,
                    SetupCost = dbTest.SetupCost,
                    InternalCost = dbTest.InternalCost,
                    ExternalCost = dbTest.ExternalCost,
                    Cost = cost,
                    SubTotal = costAndQuantity,
                    Total = costAndQuantity + dbTest.SetupCost
                });
            }

            return calcualtedTests.ToArray();
        }
        public async Task PopulateOrder(OrderSaveModel model, Order orderToUpdate)
        {
            orderToUpdate.Project = model.Project;
            orderToUpdate.JsonDetails = JsonConvert.SerializeObject(model);
            var orderDetails = orderToUpdate.GetOrderDetails();

            var tests = await CalculateTestDetails(orderDetails);

            orderDetails.SelectedTests = tests.ToArray();
            orderDetails.Total = orderDetails.SelectedTests.Sum(x=>x.Total);

            AddAdditionalFees(orderDetails);

            orderToUpdate.SaveDetails(orderDetails);
            orderToUpdate.AdditionalEmails = string.Join(";", orderDetails.AdditionalEmails);
        }

        public void PopulateOrderWithLabDetails(OrderSaveModel model, Order orderToUpdate)
        {
            var orderDetails = orderToUpdate.GetOrderDetails();
            orderDetails.Total += orderDetails.AdjustmentAmount;
            orderToUpdate.SaveDetails(orderDetails);
        }

        public async Task SendOrderToAnlab(Order order)
        {
            //TODO: Implement this.
            return;
        }

        private static void AddAdditionalFees(OrderDetails orderDetails)
        {
            if (string.Equals(orderDetails.SampleType, "Water", StringComparison.OrdinalIgnoreCase))
            {
                if (orderDetails.FilterWater)
                {
                    orderDetails.Total += orderDetails.Quantity * (orderDetails.Payment.IsInternalClient ? 11 : 17);
                }
            }
            if (string.Equals(orderDetails.SampleType, "Soil", StringComparison.OrdinalIgnoreCase))
            {
                if (orderDetails.Grind)
                {
                    orderDetails.Total += orderDetails.Quantity * (orderDetails.Payment.IsInternalClient ? 6 : 9);
                }
                if (orderDetails.ForeignSoil)
                {
                    orderDetails.Total += orderDetails.Quantity * (orderDetails.Payment.IsInternalClient ? 9 : 14);
                }
            }
            if (string.Equals(orderDetails.SampleType, "Plant", StringComparison.OrdinalIgnoreCase))
            {
                if (orderDetails.Grind)
                {
                    orderDetails.Total += orderDetails.Quantity * (orderDetails.Payment.IsInternalClient ? 6 : 9);
                }
            }
        }
    }
}

