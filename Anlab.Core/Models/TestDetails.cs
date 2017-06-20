﻿namespace Anlab.Core.Models
{
    public class TestDetails
    {
        public string Analysis { get; set; }

        public string Code { get; set; }

        public decimal InternalCost { get; set; }

        public decimal ExternalCost { get; set; }

        // The cost to run this test for one sample
        public decimal Cost { get; set; }

        // Setup cost
        public decimal SetupCost { get; set; }

        // Cost for all runs of this sample (Cost * Quantity)
        public decimal SubTotal { get; set; }

        // SubTotal + SetupCost
        public decimal Total { get; set; }
 
    }
}