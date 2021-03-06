﻿using GenesisVision.Core.Data;
using GenesisVision.Core.Data.Models;
using GenesisVision.Core.Services.Validators;
using GenesisVision.Core.Services.Validators.Interfaces;
using GenesisVision.Core.ViewModels.Investment;
using GenesisVision.Core.ViewModels.Manager;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace GenesisVision.Core.Tests.Validators
{
    [TestFixture]
    public class ManagerValidatorTests
    {
        private IManagerValidator managerValidator;

        private ApplicationDbContext context;

        private IPrincipal user;
        private AspNetUsers aspNetUser;
        private Brokers broker;
        private BrokerTradeServers brokerTradeServer;
        private ManagerAccounts managerAccountWithProgram;
        private ManagerAccounts managerAccount;
        private InvestmentPrograms investmentPrograms;
        
        [SetUp]
        public void Init()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase("databaseManagerValidator");
            context = new ApplicationDbContext(optionsBuilder.Options);

            aspNetUser = new AspNetUsers
                         {
                             Id = Guid.NewGuid(),
                             AccessFailedCount = 0,
                             Email = "test@test.com",
                             EmailConfirmed = true,
                         };
            broker = new Brokers
                     {
                         Id = Guid.NewGuid(),
                         Description = string.Empty,
                         IsEnabled = true,
                         Name = "Broker #1",
                         RegistrationDate = DateTime.Now
                     };
            brokerTradeServer = new BrokerTradeServers
                                {
                                    Id = Guid.NewGuid(),
                                    Name = "Server #1",
                                    IsEnabled = true,
                                    Host = string.Empty,
                                    RegistrationDate = DateTime.Now,
                                    Type = BrokerTradeServerType.MetaTrader4,
                                    BrokerId = broker.Id
                                };
            managerAccount = new ManagerAccounts
                             {
                                 Id = Guid.NewGuid(),
                                 BrokerTradeServerId = brokerTradeServer.Id,
                                 Description = string.Empty,
                                 IsEnabled = true,
                                 Name = "Manager",
                                 Avatar = string.Empty,
                                 Currency = "USD",
                                 Login = "111111",
                                 Rating = 0m,
                                 RegistrationDate = DateTime.Now,
                                 UserId = aspNetUser.Id
                             };
            managerAccountWithProgram = new ManagerAccounts
                                        {
                                            Id = Guid.NewGuid(),
                                            BrokerTradeServerId = brokerTradeServer.Id,
                                            Description = string.Empty,
                                            IsEnabled = true,
                                            Name = "Manager",
                                            Avatar = string.Empty,
                                            Currency = "USD",
                                            Login = "111111",
                                            Rating = 0m,
                                            RegistrationDate = DateTime.Now,
                                            UserId = aspNetUser.Id
                                        };
            investmentPrograms = new InvestmentPrograms
                                 {
                                     Id = Guid.NewGuid(),
                                     ManagersAccountId = managerAccountWithProgram.Id,
                                     DateFrom = DateTime.Now.AddDays(-10),
                                     DateTo = DateTime.Now.AddDays(10),
                                     FeeEntrance = 100m,
                                     FeeSuccess = 120m,
                                     FeeManagement = 10m,
                                     Description = "Test inv",
                                     IsEnabled = true,
                                     Period = 35,
                                     InvestMinAmount = 500,
                                     InvestMaxAmount = 1500
                                 };

            context.Add(aspNetUser);
            context.Add(broker);
            context.Add(brokerTradeServer);
            context.Add(managerAccountWithProgram);
            context.Add(managerAccount);
            context.Add(investmentPrograms);
            context.SaveChanges();

            user = new ClaimsPrincipal();

            managerValidator = new ManagerValidator(context);
        }

        [Test]
        public void ValidateNewManagerAccountRequestCheckTradeServer()
        {
            const string errorMsg = "Does not find trade server";

            var res1 = managerValidator.ValidateNewManagerAccountRequest(user,
                new NewManagerRequest {BrokerTradeServerId = Guid.NewGuid()});
            Assert.IsTrue(res1.Any(x => x.Contains(errorMsg)));

            var res2 = managerValidator.ValidateNewManagerAccountRequest(user,
                new NewManagerRequest {BrokerTradeServerId = brokerTradeServer.Id});
            Assert.IsTrue(!res2.Any(x => x.Contains(errorMsg)));
        }

        [Test]
        public void ValidateNewManagerAccountRequestCheckUser()
        {
            const string errorMsg = "Does not find user";

            var res1 = managerValidator.ValidateNewManagerAccountRequest(user,
                new NewManagerRequest
                {
                    BrokerTradeServerId = brokerTradeServer.Id,
                    UserId = aspNetUser.Id
                });
            Assert.IsTrue(res1.All(x => x != errorMsg));

            var res2 = managerValidator.ValidateNewManagerAccountRequest(user,
                new NewManagerRequest
                {
                    BrokerTradeServerId = brokerTradeServer.Id,
                    UserId = Guid.NewGuid()
                });
            Assert.IsTrue(res2.Any(x => x == errorMsg));

            var res3 = managerValidator.ValidateNewManagerAccountRequest(user,
                new NewManagerRequest
                {
                    BrokerTradeServerId = brokerTradeServer.Id,
                    UserId = null
                });
            Assert.IsTrue(res3.All(x => x != errorMsg));
        }

        [Test]
        public void ValidateNewManagerAccountRequestCheckName()
        {
            const string errorMsg = "'Name' is empty";

            var res1 = managerValidator.ValidateNewManagerAccountRequest(user,
                new NewManagerRequest
                {
                    BrokerTradeServerId = brokerTradeServer.Id,
                    UserId = aspNetUser.Id,
                    Name = "Manager #1"
                });
            Assert.IsTrue(res1.All(x => x != errorMsg));

            var res2 = managerValidator.ValidateNewManagerAccountRequest(user,
                new NewManagerRequest
                {
                    BrokerTradeServerId = brokerTradeServer.Id,
                    UserId = aspNetUser.Id,
                    Name = ""
                });
            Assert.IsTrue(res2.Any(x => x == errorMsg));
        }

        [Test]
        public void ValidateInvestSuccess()
        {
            var createInv = new CreateInvestment
                            {
                                ManagersAccountId = managerAccount.Id,
                                Description = "Test_test",
                                DateFrom = DateTime.Now.AddDays(1),
                                DateTo = DateTime.Now.AddDays(36),
                                InvestMaxAmount = 99999,
                                InvestMinAmount = 100,
                                FeeSuccess = 10,
                                FeeManagement = 20,
                                FeeEntrance = 30,
                                Period = 35
                            };

            var result = managerValidator.ValidateCreateInvestmentProgram(user, createInv);
            Assert.IsEmpty(result);
        }

        [Test]
        public void ValidateInvestWrongInvestments()
        {
            var createInv = new CreateInvestment {ManagersAccountId = Guid.NewGuid()};
            var result = managerValidator.ValidateCreateInvestmentProgram(user, createInv);
            Assert.IsTrue(result.Any(x => x.Contains("Does not find manager account")));
        }

        [Test]
        public void ValidateInvestAlreadyExist()
        {
            var createInv = new CreateInvestment {ManagersAccountId = managerAccountWithProgram.Id};
            var result = managerValidator.ValidateCreateInvestmentProgram(user, createInv);
            Assert.IsTrue(result.Any(x => x.Contains("Manager has active investment program")));
        }

        [Test]
        public void ValidateInvestWrongDates()
        {
            var createInv = new CreateInvestment
                            {
                                ManagersAccountId = managerAccount.Id,
                                Description = "Test_test",
                                DateFrom = DateTime.Now.AddDays(1),
                                DateTo = DateTime.Now.AddDays(10),
                                InvestMaxAmount = 99999,
                                InvestMinAmount = 100,
                                FeeSuccess = 10,
                                FeeManagement = 20,
                                FeeEntrance = 30,
                                Period = 35
                            };

            createInv.DateFrom = createInv.DateTo = DateTime.Now.Date;
            var result1 = managerValidator.ValidateCreateInvestmentProgram(user, createInv);
            Assert.True(result1.Any(x => x == "DateFrom must be greater than today"));
            Assert.True(result1.Any(x => x == "DateTo must be greater DateFrom"));
            
            createInv.DateFrom = DateTime.Now.AddDays(10);
            createInv.DateTo = DateTime.Now.Date;
            var result2 = managerValidator.ValidateCreateInvestmentProgram(user, createInv);
            Assert.IsTrue(result2.Any(x => x == "DateTo must be greater DateFrom"));
            
            createInv.DateFrom = createInv.DateTo = DateTime.Now.AddDays(10);
            var result3 = managerValidator.ValidateCreateInvestmentProgram(user, createInv);
            Assert.IsTrue(result3.Any(x => x == "DateTo must be greater DateFrom"));

            createInv.DateFrom = DateTime.Now.Date.AddDays(10);
            createInv.DateTo = DateTime.Now.Date.AddDays(10).AddHours(1);
            var result4 = managerValidator.ValidateCreateInvestmentProgram(user, createInv);
            Assert.True(result4.Any(x => x == "Minimum duration is 1 day"));
        }

        [Test]
        public void ValidateInvestWrongPeriod()
        {
            var createInv = new CreateInvestment
                            {
                                ManagersAccountId = managerAccount.Id,
                                Period = -1
                            };
            var result = managerValidator.ValidateCreateInvestmentProgram(user, createInv);
            Assert.IsTrue(result.Any(x => x.Contains("Period must be greater than zero")));
        }

        [Test]
        public void ValidateInvestWrongFee()
        {
            var createInv = new CreateInvestment
                            {
                                ManagersAccountId = managerAccount.Id,
                                Period = 10,
                                FeeEntrance = -1,
                                FeeSuccess = -10,
                                FeeManagement = -20
                            };
            var result = managerValidator.ValidateCreateInvestmentProgram(user, createInv);
            Assert.IsTrue(result.Any(x => x.Contains("FeeEntrance must be greater or equal zero")));
            Assert.IsTrue(result.Any(x => x.Contains("FeeSuccess must be greater or equal zero")));
            Assert.IsTrue(result.Any(x => x.Contains("FeeManagement must be greater or equal zero")));
        }

        [Test]
        public void ValidateGetManagerDetails()
        {
            var result1 = managerValidator.ValidateGetManagerDetails(user, managerAccount.Id);
            Assert.IsEmpty(result1);
            
            var result2 = managerValidator.ValidateGetManagerDetails(user, Guid.NewGuid());
            Assert.IsNotEmpty(result2);
        }

        [Test]
        public void ValidateCreateManagerAccount()
        {
            var res1 = managerValidator.ValidateCreateManagerAccount(user, new NewManager {Login = "xxxxx", RequestId = Guid.NewGuid()});
            Assert.IsTrue(res1.Any(x => x.Contains("Does not find request")));

            var requestId = Guid.NewGuid();
            context.Add(new ManagerAccountRequests {Id = requestId, UserId = aspNetUser.Id, Status = ManagerRequestStatus.Declined});
            context.SaveChanges();

            var res2 = managerValidator.ValidateCreateManagerAccount(user, new NewManager {Login = "xxxxx", RequestId = requestId});
            Assert.IsTrue(res2.Any(x => x.Contains("Could not proccess request")));

            requestId = Guid.NewGuid();
            context.Add(new ManagerAccountRequests {Id = requestId, UserId = aspNetUser.Id, Status = ManagerRequestStatus.Processed});
            context.SaveChanges();

            var res3 = managerValidator.ValidateCreateManagerAccount(user, new NewManager {Login = "xxxxx", RequestId = requestId});
            Assert.IsTrue(res3.Any(x => x.Contains("Could not proccess request")));

            requestId = Guid.NewGuid();
            context.Add(new ManagerAccountRequests {Id = requestId, UserId = aspNetUser.Id, Status = ManagerRequestStatus.Created});
            context.SaveChanges();

            var res4 = managerValidator.ValidateCreateManagerAccount(user, new NewManager {Login = "xxxxx", RequestId = requestId});
            Assert.IsEmpty(res4);
        }
    }
}
