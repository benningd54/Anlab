using System.Threading.Tasks;
using Anlab.Core.Domain;
using Anlab.Core.Services;
using Microsoft.Extensions.Options;

namespace AnlabMvc.Services
{
    public interface IOrderMessageService
    {
        Task EnqueueCreatedMessage(Order order);
        Task EnqueueReceivedMessage(Order order, bool bypass = false);
        Task EnqueueFinalizedMessage(Order order);
        Task EnqueuePaidMessage(Order order);
        Task EnqueueBillingMessage(Order order, string subject = "Anlab Work Order Billing Info");

    }

    public class OrderMessageService : IOrderMessageService
    {
        private readonly ViewRenderService _viewRenderService;
        private readonly IMailService _mailService;
        private readonly AppSettings _appSettings;

        public OrderMessageService(ViewRenderService viewRenderService, IMailService mailService, IOptions<AppSettings> appSettings)
        {
            _viewRenderService = viewRenderService;
            _mailService = mailService;
            _appSettings = appSettings.Value;
        }
        public async Task EnqueueCreatedMessage(Order order)
        {
            var body = await _viewRenderService.RenderViewToStringAsync("Templates/_OrderCreated", order);

            var message = new MailMessage
            {
                Subject = "Work Order Confirmation",
                Body = body,
                SendTo = GetSendTo(order),
                Order = order,
                User = order.Creator,
            };

            _mailService.EnqueueMessage(message);
        }

        private string GetSendTo(Order order)
        {
            var sendTo = order.Creator.Email;
            if (!string.IsNullOrWhiteSpace(order.AdditionalEmails))
            {
                sendTo = $"{sendTo};{order.AdditionalEmails}";
            }

            return sendTo;
        }

        public async Task EnqueueReceivedMessage(Order order, bool bypass = false)
        {
            //TODO: change body of email, right now it is the same as OrderCreated
            var body = await _viewRenderService.RenderViewToStringAsync("Templates/_OrderReceived", order);

            if (bypass)
            {
                body = $"Email not sent to clients. </br> {GetSendTo(order)} </br></br></br> {body}";
            }

            var message = new MailMessage
            {
                Subject = "Order Received Confirmation",
                Body = body,
                SendTo = GetSendTo(order),
                Order = order,
                User = order.Creator,
            };

            if (bypass)
            {
                message.Subject = "Order Received Confirmation -- Bypass Client";
                message.SendTo = "anlab@test.com"; //TODO: Replace
            }

            _mailService.EnqueueMessage(message);
        }

        public async Task EnqueueFinalizedMessage(Order order)
        {
            var orderDetails = order.GetOrderDetails();
            var subject = "Order Finalized - Awaiting Payment";
            //TODO: change body of email, right now it is the same as OrderCreated
            var body = await _viewRenderService.RenderViewToStringAsync("Templates/_OrderFinalized", order);

            var message = new MailMessage
            {
                Subject = subject,
                Body = body,
                SendTo = GetSendTo(order),
                Order = order,
                User = order.Creator,
            };

            _mailService.EnqueueMessage(message);
        }

        public async Task EnqueuePaidMessage(Order order)
        {
            var orderDetails = order.GetOrderDetails();
            var subject = "Order Payment Complete";
            //TODO: change body of email, right now it is the same as OrderCreated
            var body = await _viewRenderService.RenderViewToStringAsync("Templates/_PaymentReceived", order);

            var message = new MailMessage
            {
                Subject = subject,
                Body = body,
                SendTo = GetSendTo(order),
                Order = order,
                User = order.Creator,
            };

            _mailService.EnqueueMessage(message);
        }

        public async Task EnqueueBillingMessage(Order order, string subject = "Anlab Work Order Billing Info")
        {
            var orderDetails = order.GetOrderDetails();
            //TODO: change wording, change SendTo to billing email
            var body = await _viewRenderService.RenderViewToStringAsync("Templates/_BillingInformation", order);


            var message = new MailMessage
            {
                Subject = subject,
                Body = body,
                SendTo = _appSettings.AccountsEmail,
                Order = order,
                User = order.Creator,
            };

            _mailService.EnqueueMessage(message);
        }

    }
}
