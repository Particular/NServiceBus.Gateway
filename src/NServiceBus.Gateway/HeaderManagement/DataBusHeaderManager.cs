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

        public IDictionary<string, string> Reassemble(string clientId, IDictionary<string, string> input)
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
                    var message = string.Format("Expected {0} databus properties. None were received. Please resubmit.",expectedDatabusProperties.Count);
                    throw new ChannelException(412,message);
                }

                foreach (var propertyHeader in expectedDatabusProperties)
                {
                    string propertyValue;
                    if (!collection.TryGetValue(propertyHeader.Key, out propertyValue))
                    {
                        var message = string.Format("Databus property {0} was never received. Please resubmit.",propertyHeader.Key);
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