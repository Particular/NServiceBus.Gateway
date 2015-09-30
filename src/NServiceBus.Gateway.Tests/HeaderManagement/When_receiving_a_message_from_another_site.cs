//namespace NServiceBus.Gateway.Tests.HeaderManagement
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Threading.Tasks;
//    using MessageMutator;
//    using NUnit.Framework;

    // TODO: Fix this
    //[TestFixture]
    //public class When_receiving_a_message_from_another_site
    //{
    //    string addressOfOriginatingEndpoint;
    //    Dictionary<string, string> incomingHeaders;
    //    Dictionary<string, string> outgoingHeaders;
    //    const string originatingSite = "SiteA";
    //    const string idOfIncomingMessage = "xyz";

    //    [SetUp]
    //    public void SetUp()
    //    {
    //        addressOfOriginatingEndpoint = "EndpointLocatedInSiteA";

    //        incomingHeaders = new Dictionary<string, string>
    //        {
    //            [Headers.OriginatingSite] = originatingSite,
    //            [Headers.HttpFrom] = originatingSite,
    //            [Headers.ReplyToAddress] = addressOfOriginatingEndpoint,
    //            [Headers.MessageId] = Guid.NewGuid().ToString(),
    //        };

    //        gatewayHeaderManager = new GatewayHeaderManager();

    //        gatewayHeaderManager.MutateIncoming(new MutateIncomingTransportMessageContext(null, incomingHeaders));

    //        outgoingHeaders = new Dictionary<string, string>
    //        {
    //            {Headers.CorrelationId, idOfIncomingMessage}
    //        };
    //    }
       
    //    [Test]
    //    public async Task Should_use_the_originating_siteKey_as_destination_for_response_messages()
    //    {      
    //        await gatewayHeaderManager.MutateOutgoing(new MutateOutgoingTransportMessageContext(null, null, outgoingHeaders, null, incomingHeaders));

    //        Assert.AreEqual(outgoingHeaders[Headers.HttpTo], originatingSite);
    //    }

    //    [Test]
    //    public async Task Should_route_the_response_to_the_replyTo_address_specified_in_the_incoming_message()
    //    {
    //        await gatewayHeaderManager.MutateOutgoing(new MutateOutgoingTransportMessageContext(null, null, outgoingHeaders, null, incomingHeaders));

    //        Assert.AreEqual(outgoingHeaders[Headers.RouteTo], addressOfOriginatingEndpoint);
    //    }
    //}
//}