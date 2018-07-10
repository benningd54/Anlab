using System;
using System.Collections.Generic;
using System.Text;

namespace Anlab.Core.Models
{
    public class ClientInfo
    {
        public string ClientId { get; set; }
        public string Email { get; set; }
        public string Employer { get; set; }
        public string Department { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string CopyPhone { get; set; }

        public string PiName { get; set; }
        public string PiEmail { get; set; }
    }
}
