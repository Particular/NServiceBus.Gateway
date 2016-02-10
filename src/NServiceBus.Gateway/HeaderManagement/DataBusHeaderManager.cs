namespace NServiceBus.Gateway.HeaderManagement
{
    using System.Collections.Generic;
    using System.Linq;
    using Receiving;

    class DataBusHeaderManager
    {
        public void InsertHeader(string clientId, string headerKey, string headerValue)
        {
            lock (headers)
            {
                Dictionary<string, string> collection;
                if (!headers.TryGetValue(clientId, out collection))
                {
                    collection = new Dictionary<string, string>();
                    headers[clientId] = collection;
                }
                collection[headerKey] = headerValue;
            }
        }

        public IDictionary<string, string> ReassembleDataBusProperties(string clientId, IDictionary<string, string> input)
        {
            var expectedDatabusProperties = input.Where(kv => kv.Key.Contains("NServiceBus.DataBus.")).ToList();

            if (!expectedDatabusProperties.Any())
            {
                return input;
            }

            lock (headers)
            {
                Dictionary<string, string> collection;
                if (!headers.TryGetValue(clientId, out collection))
                {
                    var message = $"Expected {expectedDatabusProperties.Count} databus properties. None were received. Please resubmit.";
                    throw new ChannelException(412,message);
                }

                foreach (var propertyHeader in expectedDatabusProperties)
                {
                    string propertyValue;
                    if (!collection.TryGetValue(propertyHeader.Key, out propertyValue))
                    {
                        var message = $"Databus property {propertyHeader.Key} was never received. Please resubmit.";
                        throw new ChannelException(412,message);
                    }
                    input[propertyHeader.Key] = propertyValue;
                }

                headers.Remove(clientId);
            }
            return input;
        }

        Dictionary<string, Dictionary<string, string>> headers 
            = new Dictionary<string, Dictionary<string, string>>();
    }
}