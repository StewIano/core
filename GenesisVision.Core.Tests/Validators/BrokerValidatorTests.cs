﻿using GenesisVision.Core.Data;
using GenesisVision.Core.Data.Models;
using GenesisVision.Core.Services.Validators;
using GenesisVision.Core.Services.Validators.Interfaces;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace GenesisVision.Core.Tests.Validators
{
    [TestFixture]
    public class BrokerValidatorTests
    {
        private IBrokerValidator brokerValidator;

        private ApplicationDbContext context;

        private IPrincipal user;
        private Brokers broker;
        private BrokerTradeServers brokerTradeServer;

        [SetUp]
        public void Init()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase("databaseBrokerValidator");
            context = new ApplicationDbContext(optionsBuilder.Options);

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
            context.Add(broker);
            context.Add(brokerTradeServer);
            context.SaveChanges();

            user = new ClaimsPrincipal();

            brokerValidator = new BrokerValidator(context);
        }

        [Test]
        public void ValidateGetBrokerInitDataServerExists()
        {
            var res1 = brokerValidator.ValidateGetBrokerInitData(user, brokerTradeServer.Id);
            Assert.IsEmpty(res1);

            var res2 = brokerValidator.ValidateGetBrokerInitData(user, Guid.NewGuid());
            Assert.IsTrue(res2.Any(x => x.Contains("does not exist")));
        }

        [Test]
        public void ValidateGetBrokerInitDataServerEnable()
        {
            const string error = "Access denied";

            context.Brokers.First().IsEnabled = false;
            context.SaveChanges();

            var res1 = brokerValidator.ValidateGetBrokerInitData(user, brokerTradeServer.Id);
            Assert.IsTrue(res1.Any(x => x.Contains(error)));

            context.Brokers.First().IsEnabled = true;
            context.BrokerTradeServers.First().IsEnabled = false;
            context.SaveChanges();

            var res2 = brokerValidator.ValidateGetBrokerInitData(user, brokerTradeServer.Id);
            Assert.IsTrue(res2.Any(x => x.Contains(error)));
        }
    }
}
