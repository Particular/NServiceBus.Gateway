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

    public class When_listening_on_multiple_uris : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_on_all_uris()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Headquarters>(b => b.When(async bus =>
                {
                    var hostname = Dns.GetHostName();

                    await SendMessage(DefaultReceiveURI);
                    await SendMessage($"http://{hostname}:25898/Headquarters/");
                }))
                .Done(c => c.GotMessageOnDefaultChannel && c.GotMessageOnNonDefaultChannel)
                .Run();

            Assert.IsTrue(context.GotMessageOnDefaultChannel);
            Assert.IsTrue(context.GotMessageOnNonDefaultChannel);
        }

        async Task SendMessage(string url)
        {
            while (true)
            {
                try
                {
                    var webRequest = CreateWebRequest(url);
                    using (var httpClient = new HttpClient())
                    using (var response = await httpClient.SendAsync(webRequest))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            break;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                }
            }
        }

        static HttpRequestMessage CreateWebRequest(string uri)
        {
            var webRequest = new HttpRequestMessage(HttpMethod.Post, uri);

            const string message = "<?xml version=\"1.0\" ?><Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://tempuri.net/NServiceBus.Gateway.AcceptanceTests\"><MyRequest></MyRequest></Messages>";

            var messagePayload = new MemoryStream(Encoding.UTF8.GetBytes(message));
            webRequest.Content = new StreamContent(messagePayload);
            webRequest.Content.Headers.Add("Content-MD5", Hash(messagePayload));

            webRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml; charset=utf-8");

            webRequest.Content.Headers.Add("Content-Encoding", "utf-8");
            webRequest.Content.Headers.Add("NServiceBus.CallType", "SingleCallSubmit");
            webRequest.Content.Headers.Add("NServiceBus.AutoAck", "true");
            webRequest.Content.Headers.Add("SentToHeader", uri);
            webRequest.Content.Headers.Add("NServiceBus.Id", Guid.NewGuid().ToString("N"));

            return webRequest;
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
            public bool GotMessageOnDefaultChannel { get; set; }
            public bool GotMessageOnNonDefaultChannel { get; set; }
        }

        public class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel(DefaultReceiveURI);
                    gatewaySettings.AddReceiveChannel($"http://{Dns.GetHostName()}:25898/Headquarters/");
                })
                .IncludeType<MyRequest>();
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                Context testContext;

                public MyRequestHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyRequest response, IMessageHandlerContext context)
                {
                    if (context.MessageHeaders["SentToHeader"] == DefaultReceiveURI)
                    {
                        testContext.GotMessageOnDefaultChannel = true;
                    }
                    else
                    {
                        testContext.GotMessageOnNonDefaultChannel = true;
                    }

                    return Task.FromResult(0);
                }
            }
        }

        static string DefaultReceiveURI = "http://localhost:25898/Headquarters/";
    }
}