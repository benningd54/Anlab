﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Anlab.Core.Data;
using Anlab.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Anlab.Jobs.MoneyMovement
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        public static IServiceProvider Provider { get; set; }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");


            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.Equals(environmentName, "development", StringComparison.OrdinalIgnoreCase))
            {
                builder.AddUserSecrets<Program>();
            }


            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
            

            IServiceCollection services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite("Data Source=..\\..\\..\\..\\Anlab.Mvc\\bin\\Debug\\netcoreapp1.1\\anlab.db")
                // options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
            );
            Provider = services.BuildServiceProvider();


            var result = Task.Run(() => ProcessOrders()).Result; //Wasn't able to debug this unless it returned a result...



            Console.WriteLine("Done");
        }

        public static async Task<bool> ProcessOrders()
        {
            var dbContext = Provider.GetService<ApplicationDbContext>();
            var token = Configuration.GetSection("MoneyMovement:SlothApiKey").Value;
            var url = Configuration.GetSection("MoneyMovement:SlothApiUrl").Value;


            using (var client = new HttpClient())
            {
                var orders = dbContext.Orders.Include(i => i.ApprovedPayment).Where(a =>
                    a.ApprovedPayment != null && a.Paid && a.Status != OrderStatusCodes.Complete).ToList();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Add("X-Auth-Token", token);

                if (orders?.Count > 0)
                {
                    foreach (var order in orders)
                    {
                        var response = await client.GetAsync(order.ApprovedPayment.Transaction_Id);
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            continue;
                        }
                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            continue;
                        }
                        if (response.IsSuccessStatusCode)
                        {
                            //TODO: If response is 200, extract out KFS, write to ApprovePayment Field, update order status to complete  
                            var xxx = await response.GetContentOrNullAsync<IEnumerable<string>>();
                        }

                    }

                    await dbContext.SaveChangesAsync();
                }

            }

            return true;



            //try
            //{
            //    var dbContext = Provider.GetService<ApplicationDbContext>();

            //    //Approved payment will only have a value when a CC payment is used.
            //    var orders = dbContext.Orders.Include(i => i.ApprovedPayment).Where(a =>
            //        a.ApprovedPayment != null && a.Paid && a.Status != OrderStatusCodes.Complete).ToList();
            //    var test = dbContext.Orders.Include(i => i.ApprovedPayment).First(a => a.ApprovedPayment != null);
            //    var testId = test.ApprovedPayment.Transaction_Id;

            //    var xxx = Configuration.GetSection("MoneyMovement:SlothApiKey").Value;
            //    var yyy = Configuration.GetSection("MoneyMovement:SlothApiUrl").Value;

            //    using (var client = new HttpClient())
            //    {
            //        client.BaseAddress = new Uri(yyy);
            //        client.DefaultRequestHeaders.Add("X-Auth-Token", xxx);
            //        var response = await client.GetAsync(testId);
            //        //var content = response.Content;
            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //    throw;
            //}
            return true;
        }

        public static async Task<T> GetContentOrNullAsync<T>(this HttpResponseMessage response)
        {
            // return null on 404
            if (response.StatusCode == HttpStatusCode.NotFound)
                return default(T);

            // check for success
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsAsync<T>();

            throw new Exception();
        }


    }
}
