namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_listening_on_multiple_uris : NServiceBusAcceptanceTest
    {
        [SetUp]
        public void Init()
        {
            hostname = Environment.GetEnvironmentVariable("COMPUTERNAME") ?? Environment.GetEnvironmentVariable("HOSTNAME");
        }

        [Test]
        public async Task Should_process_message_on_first_uri()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Headquarters>(b => b.When(bus =>
                {
                    var webRequest = CreateWebRequest("http://localhost:25898/Headquarters/");

                    while (true)
                    {
                        try
                        {
                            using (var myWebResponse = (HttpWebResponse)webRequest.GetResponse())
                            {
                                if (myWebResponse.StatusCode == HttpStatusCode.OK)
                                {
                                    break;
                                }
                            }
                        }
                        catch (WebException)
                        {
                        }
                    }
                    return Task.FromResult(0);
                }))
                .Done(c => c.GotMessage)
                .Run();

            Assert.IsTrue(context.GotMessage);
            Assert.AreEqual("http://localhost:25898/Headquarters/", context.SentToHeader);
            context.GotMessage = false;
        }

        [Test]
        public async Task Should_process_message_on_second_uri()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Headquarters>(b => b.When(bus =>
                {
                    var webRequest = CreateWebRequest($"http://{hostname}:25898/Headquarters/");

                    while (true)
                    {
                        try
                        {
                            using (var myWebResponse = (HttpWebResponse)webRequest.GetResponse())
                            {
                                if (myWebResponse.StatusCode == HttpStatusCode.OK)
                                {
                                    break;
                                }
                            }
                        }
                        catch (WebException)
                        {
                        }
                    }
                    return Task.FromResult(0);
                }))
                .Done(c => c.GotMessage)
                .Run();

            Assert.IsTrue(context.GotMessage);
            Assert.AreEqual($"http://{hostname}:25898/Headquarters/", context.SentToHeader);
        }

        static HttpWebRequest CreateWebRequest(string uri)
        {
#pragma warning disable DE0003 // API is deprecated
            var webRequest = (HttpWebRequest)WebRequest.Create(uri);
#pragma warning restore DE0003 // API is deprecated
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml; charset=utf-8";
            webRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1)";

            webRequest.Headers.Add("Content-Encoding", "utf-8");
            webRequest.Headers.Add("NServiceBus.CallType", "SingleCallSubmit");
            webRequest.Headers.Add("NServiceBus.AutoAck", "true");
            webRequest.Headers.Add("SentToHeader", uri);
            webRequest.Headers.Add("NServiceBus.Id", Guid.NewGuid().ToString("N"));

            const string message = "<?xml version=\"1.0\" ?><Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://tempuri.net/NServiceBus.Gateway.AcceptanceTests\"><MyRequest></MyRequest></Messages>";

            using (var messagePayload = new MemoryStream(Encoding.UTF8.GetBytes(message)))
            {
                webRequest.Headers.Add(HttpRequestHeader.ContentMd5, HttpUtility.UrlEncode(Hash(messagePayload)));
                webRequest.ContentLength = messagePayload.Length;

                using (var requestStream = webRequest.GetRequestStream())
                {
                    messagePayload.CopyTo(requestStream);
                }
            }

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
            public bool GotMessage { get; set; }

            public string SentToHeader { get; set; }
        }

        public class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    c.Gateway().AddReceiveChannel("http://localhost:25898/Headquarters/");
                    c.Gateway().AddReceiveChannel($"http://{hostname}:25898/Headquarters/");
                })
                .IncludeType<MyRequest>();
            }

            public class MyResponseHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public Task Handle(MyRequest response, IMessageHandlerContext context)
                {
                    Context.GotMessage = true;
                    Context.SentToHeader = context.MessageHeaders["SentToHeader"];
                    return Task.FromResult(0);
                }
            }
        }

        static string hostname;
    }
}