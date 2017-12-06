using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace Anlab.Core.Models
{
    public class OtherPaymentInfo
    {
        public string PaymentType { get; set; }
        
        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }
        
        [Required]
        [Display(Name = "Account Contact Name")]
        public string AcName { get; set; }
        
        [Required]
        [Display(Name = "Account Contact Address")]
        public string AcAddr { get; set; }
        
        [Required]
        [EmailAddress]
        [Display(Name = "Account Contact Email")]
        public string AcEmail { get; set; }
        
        [Phone]
        [Required]
        [Display(Name = "Account Contact Phone Number")]
        public string AcPhone { get; set; }
        
        //[Required] 
        [Display(Name = "PO #")]
        public string PoNum { get; set; }
    }
}