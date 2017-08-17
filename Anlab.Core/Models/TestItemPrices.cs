﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Anlab.Core.Domain;

namespace Anlab.Core.Models
{

    public class TestItemPrices
    {
        public string Id { get; set; }
        public decimal Cost { get; set; }
        public decimal SetupCost { get; set; } = 30; //TODO: A better way to set or Default this.
        public string Name { get; set; }
        public int Multiplier { get; set; }

        public decimal SetupPrice => SetupCost * Multiplier;
    }

}