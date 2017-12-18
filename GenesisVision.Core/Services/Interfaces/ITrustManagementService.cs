﻿using System;
using System.Collections.Generic;
using GenesisVision.Core.Models;
using GenesisVision.Core.ViewModels.Investment;
using GenesisVision.Core.ViewModels.Manager;

namespace GenesisVision.Core.Services.Interfaces
{
    public interface ITrustManagementService
    {
        OperationResult<Guid> CreateInvestmentProgram(CreateInvestment investment);
        OperationResult Invest(Invest model);
        OperationResult<List<Investment>> GetInvestments(InvestmentsFilter filter);
    }
}
