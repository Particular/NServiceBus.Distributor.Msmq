namespace NServiceBus.Distributor.MSMQ.AcceptanceTests
{
    using System;
    using System.Messaging;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Transports.Msmq;

    [TestFixture]
    public class When_message_is_processed_after_triggering_SLR : NServiceBusAcceptanceTest
    {
        static TimeSpan SlrDelay = TimeSpan.FromSeconds(5);

        [Test]
        public void Worker_sends_a_ready_message_to_the_distributor()
        {
            try
            {
                var queue = new MessageQueue(MsmqUtilities.GetFullPath(Address.Parse("messageisprocessedaftertriggeringSLR.distributor.msmq.distributor.storage")), false, true, QueueAccessMode.Receive);
                queue.Purge();
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
                //NOOP
            }
            Scenario.Define(() => new Context
            {
                Id = Guid.NewGuid()
            })
                .WithEndpoint<Client>(b => b
                    .Given((bus, context) => bus.Send(new MyMessage
                    {
                        Id = context.Id
                    }))
                    )
                .WithEndpoint<Distributor>()
                .WithEndpoint<Worker>()
                .Done(c => c.SecondAttemptSucceeded)
                .Repeat(r => r.For(Transports.Default))
                .Should(context =>
                {
                    Assert.IsTrue(context.FirstAttemptFailed);
                    Assert.IsTrue(context.SecondAttemptSucceeded);
                })
                .Run();
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
                        c.DistributorControlAddress = "MessageIsProcessedAfterTriggeringSLR.Distributor.Msmq.distributor.control";
                        c.DistributorDataAddress = "MessageIsProcessedAfterTriggeringSLR.Distributor.Msmq";
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
