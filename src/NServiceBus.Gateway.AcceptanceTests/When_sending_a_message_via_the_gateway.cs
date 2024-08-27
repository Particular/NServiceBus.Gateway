namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_sending_a_message_via_the_gateway : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_process_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Headquarters>(b => b.When(async bus =>
                {
                    var webRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:25898/Headquarters/");

                    const string message = "<?xml version=\"1.0\" ?><Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://tempuri.net/NServiceBus.Gateway.AcceptanceTests\"><MyRequest></MyRequest></Messages>";

                    var messagePayload = new MemoryStream(Encoding.UTF8.GetBytes(message));
                    webRequest.Content = new StreamContent(messagePayload);
                    webRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml; charset=utf-8");
                    webRequest.Content.Headers.Add("Content-MD5", Hash(messagePayload));

                    webRequest.Content.Headers.Add("Content-Encoding", "utf-8");
                    webRequest.Content.Headers.Add("NServiceBus.CallType", "SingleCallSubmit");
                    webRequest.Content.Headers.Add("NServiceBus.AutoAck", "true");
                    webRequest.Content.Headers.Add("MySpecialHeader", "MySpecialValue");
                    webRequest.Content.Headers.Add("NServiceBus.Id", Guid.NewGuid().ToString("N"));

                    while (true)
                    {
                        try
                        {
                            using (var httpClient = new HttpClient())
                            using (var response = await httpClient.SendAsync(webRequest))
                            {
                                if (response.StatusCode == HttpStatusCode.OK)
                                {
                                    break;
                                }
                            }
                        }
                        catch (WebException)
                        {
                        }
                    }
                }))
                .Done(c => c.GotMessage)
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(context.GotMessage, Is.True);
                Assert.That(context.MySpecialHeader, Is.EqualTo("MySpecialValue"));
            });
        }

        static string Hash(Stream stream)
        {
            var position = stream.Position;
            var hash = MD5.Create().ComputeHash(stream);

            stream.Position = position;

            return Convert.ToBase64String(hash);
        }

        public class Context : ScenarioContext
        {
            public bool GotMessage { get; set; }

            public string MySpecialHeader { get; set; }
        }

        public class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:25898/Headquarters/");
                })
                .IncludeType<MyRequest>();
            }

            public class MyResponseHandler : IHandleMessages<MyRequest>
            {
                Context testContext;

                public MyResponseHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyRequest response, IMessageHandlerContext context)
                {
                    testContext.MySpecialHeader = context.MessageHeaders["MySpecialHeader"];
                    testContext.GotMessage = true;
                    return Task.FromResult(0);
                }
            }
        }
    }
}