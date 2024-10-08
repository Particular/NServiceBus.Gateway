﻿namespace NServiceBus.Gateway.HeaderManagement
{
    using System.Collections.Generic;
    using System.Linq;
    using Receiving;

    class ClaimCheckHeaderManager
    {
        public void InsertHeader(string clientId, string headerKey, string headerValue)
        {
            lock (headers)
            {
                if (!headers.TryGetValue(clientId, out Dictionary<string, string> collection))
                {
                    collection = [];
                    headers[clientId] = collection;
                }
                collection[headerKey] = headerValue;
            }
        }

        public IDictionary<string, string> ReassembleClaimCheckProperties(string clientId, IDictionary<string, string> input)
        {
            var expectedDatabusProperties = input.Where(kv => kv.Key.Contains("NServiceBus.DataBus.")).ToList();

            if (!expectedDatabusProperties.Any())
            {
                return input;
            }

            lock (headers)
            {
                if (!headers.TryGetValue(clientId, out Dictionary<string, string> collection))
                {
                    var message = $"Expected {expectedDatabusProperties.Count} claimcheck properties. None were received. Please resubmit.";
                    throw new ChannelException(412, message);
                }

                foreach (var propertyHeader in expectedDatabusProperties)
                {
                    if (!collection.TryGetValue(propertyHeader.Key, out string propertyValue))
                    {
                        var message = $"ClaimCheck property {propertyHeader.Key} was never received. Please resubmit.";
                        throw new ChannelException(412, message);
                    }
                    input[propertyHeader.Key] = propertyValue;
                }

                headers.Remove(clientId);
            }
            return input;
        }

        Dictionary<string, Dictionary<string, string>> headers = [];
    }
}