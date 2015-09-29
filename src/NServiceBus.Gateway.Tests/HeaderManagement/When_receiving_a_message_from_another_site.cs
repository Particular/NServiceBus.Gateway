namespace NServiceBus.Gateway.Tests.HeaderManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Gateway.HeaderManagement;
    using MessageMutator;
    using NUnit.Framework;

    [TestFixture]
    public class When_receiving_a_message_from_another_site
    {
        GatewayHeaderManager gatewayHeaderManager;
        TransportMessage incomingMessage;
        TransportMessage responseMessage;

        string addressOfOriginatingEndpoint;
        const string originatingSite = "SiteA";
        const string idOfIncomingMessage = "xyz";

        [SetUp]
        public void SetUp()
        {
            addressOfOriginatingEndpoint = "EndpointLocatedInSiteA";


            var existingHeaders = new Dictionary<string, string>
            {
                {
                    Headers.ReplyToAddress, addressOfOriginatingEndpoint.ToString()
                }
            };
            incomingMessage = new TransportMessage(Guid.NewGuid().ToString(), existingHeaders);

            incomingMessage.Headers[Headers.OriginatingSite] = originatingSite;
            incomingMessage.Headers[Headers.HttpFrom] = originatingSite;
            gatewayHeaderManager = new GatewayHeaderManager();

            gatewayHeaderManager.MutateIncoming(incomingMessage);

            responseMessage = new TransportMessage
            {
                CorrelationId = idOfIncomingMessage
            };
        }
       
        [Test]
        public async Task Should_use_the_originating_siteKey_as_destination_for_response_messages()
        {      
            await gatewayHeaderManager.MutateOutgoing(new MutateOutgoingTransportMessageContext());

            Assert.AreEqual(responseMessage.Headers[Headers.HttpTo], originatingSite);
        }

        [Test]
        public async Task Should_route_the_response_to_the_replyTo_address_specified_in_the_incoming_message()
        {
            await gatewayHeaderManager.MutateOutgoing(new MutateOutgoingTransportMessageContext());

            Assert.AreEqual(responseMessage.Headers[Headers.RouteTo], addressOfOriginatingEndpoint);
        }
    }
}