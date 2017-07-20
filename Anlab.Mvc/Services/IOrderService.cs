﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Anlab.Core.Data;
using Anlab.Core.Domain;
using Anlab.Core.Models;
using AnlabMvc.Models.Order;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AnlabMvc.Services
{
    public interface IOrderService
    {
        Task PopulateOrder(OrderSaveModel model, Order orderToUpdate);
        void PopulateOrderWithLabDetails(OrderSaveModel model, Order orderToUpdate);
        Task SendOrderToAnlab(Order order);

        Task<List<TestItemModel>> PopulateTestItemModel();

        void Test();
    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITestItemPriceService _itemPriceService;
        private readonly ConnectionSettings _connectionSettings;
        private readonly AppSettings _appSettings;

        public OrderService(ApplicationDbContext context, ITestItemPriceService itemPriceService, IOptions<AppSettings> appSettings, IOptions<ConnectionSettings> connectionSettings)
        {
            _context = context;
            _itemPriceService = itemPriceService;            
            _appSettings = appSettings.Value;
            _connectionSettings = connectionSettings.Value; //Will need to push to Anlab.
        }

        public async Task<List<TestItemModel>> PopulateTestItemModel()
        {
            var prices = await _itemPriceService.GetPrices();
            var items = _context.TestItems.AsNoTracking().ToList();

            return GetJoined(prices, items);
        }

        public void Test()
        {
            var connection = new SqlConnection(_connectionSettings.AnlabConnection);
            using (connection)
            {
                connection.Open();
                var test = connection.Query<TestItemPrices>(
                    "SELECT  [ACODE] as Code,[APRICE] as Cost,[ANAME] as 'Name',[WORKUNIT] as Multiplier FROM [ANL_LIST] where ACODE = 'DIC-W'");
                connection.Close();
            }
        }


        private async Task<IList<TestItemModel>> PopulateSelectedTestsItemModel(IEnumerable<int> selectedTestIds)
        {
            var prices = await _itemPriceService.GetPrices();
            var items = _context.TestItems.Where(a => selectedTestIds.Contains(a.Id)).AsNoTracking().ToList();

            return GetJoined(prices, items);
        }

        private List<TestItemModel> GetJoined(IList<TestItemPrices> prices, List<TestItem> items)
        {
            return (from i in items
                join p in prices on i.Code equals p.Code
                select new TestItemModel
                {
                    Analysis = i.Analysis,
                    Category = i.Category,
                    Code = i.Code,
                    ExternalCost = Math.Ceiling(p.Cost * _appSettings.NonUcRate),
                    Group = i.Group,
                    Id = i.Id,
                    InternalCost = Math.Ceiling(p.Cost),
                    ExternalSetupCost = Math.Ceiling(p.SetupPrice * _appSettings.NonUcRate),
                    InternalSetupCost = Math.Ceiling(p.SetupPrice)
                }).ToList();
        }

        private async Task<TestDetails[]> CalculateTestDetails(OrderDetails orderDetails)
        {
            // TODO: Do we really want to match on ID, or Code, or some combination?
            var selectedTestIds = orderDetails.SelectedTests.Select(t => t.Id);
            var tests = await PopulateSelectedTestsItemModel(selectedTestIds);

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
                    SetupCost = orderDetails.Payment.IsInternalClient ?  dbTest.InternalSetupCost : dbTest.ExternalSetupCost,
                    Cost = cost,
                    SubTotal = costAndQuantity,
                    Total = costAndQuantity + (orderDetails.Payment.IsInternalClient ? dbTest.InternalSetupCost : dbTest.ExternalSetupCost)
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

    }
}

