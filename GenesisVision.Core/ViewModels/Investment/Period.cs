﻿using System;
using System.Collections.Generic;
using GenesisVision.Core.Data.Models;

namespace GenesisVision.Core.ViewModels.Investment
{
    public class Period
    {
        public Guid Id { get; set; }
        public int Number { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public PeriodStatus Status { get; set; }
        public List<InvestmentRequest> InvestmentRequest { get; set; }
    }
}
