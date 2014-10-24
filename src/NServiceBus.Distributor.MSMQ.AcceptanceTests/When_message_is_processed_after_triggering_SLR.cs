namespace NServiceBus.Distributor.MSMQ.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NUnit.Framework;

    [TestFixture]
    public class When_message_is_processed_after_triggering_SLR : NServiceBusAcceptanceTest
    {
        static TimeSpan SlrDelay = TimeSpan.FromSeconds(5);

        [Test]
        public void Worker_sends_a_ready_message_to_the_distributor()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                .WithEndpoint<Client>(b => b
                    .Given((bus, c) => bus.Send(new MyMessage
                    {
                        Id = c.Id
                    }))
                    )
                .WithEndpoint<Distributor>()
                .WithEndpoint<Worker>()
                .AllowExceptions()
                .Done(c => c.SecondAttemptSucceeded)
                .Run();

            Assert.IsTrue(context.FirstAttemptFailed);
            Assert.IsTrue(context.SecondAttemptSucceeded);
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool FirstAttemptFailed { get; set; }
            public bool SecondAttemptSucceeded { get; set; }
        }

        public class Client : EndpointConfigurationBuilder
        {
            public Client()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyMessage>(typeof(Distributor));
            }
        }

        public class Distributor : EndpointConfigurationBuilder
        {
            public Distributor()
            {
                EndpointSetup<DefaultServer>(c => c.RunMSMQDistributor(false));
            }
        }

        public class Worker : EndpointConfigurationBuilder
        {
            public Worker()
            {
                EndpointSetup<DefaultServer>(c => c.EnlistWithMSMQDistributor())
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 0; //to skip the FLR
                    })
                    .WithConfig<UnicastBusConfig>(c =>
                    {
                        c.DistributorControlAddress = "MessageIsProcessedAfterTriggeringSLR.Distributor.Msmqtransport.distributor.control";
                        c.DistributorDataAddress = "MessageIsProcessedAfterTriggeringSLR.Distributor.Msmqtransport";
                    })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.NumberOfRetries = 1;
                        c.TimeIncrease = SlrDelay;
                    }).WithConfig<MasterNodeConfig>(c =>
                    {
                        c.Node = "particular.net";
                    });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage request)
                {
                    if (Context.Id != request.Id)
                    {
                        return;
                    }

                    if (!Context.FirstAttemptFailed)
                    {
                        Context.FirstAttemptFailed = true;
                        throw new Exception("Triggering SLR");
                    }
                    if (Bus.CurrentMessageContext.Headers.ContainsKey(Headers.Retries))
                    {
                        Context.SecondAttemptSucceeded = true;
                    }
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}
