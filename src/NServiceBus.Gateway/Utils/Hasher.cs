namespace NServiceBus.Gateway.Utils
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using Receiving;

    class Hasher
    {
        public static string Hash(Stream stream)//TODO should probably be awaited
        {
            var position = stream.Position;
            var hash = MD5.Create().ComputeHash(stream);

            stream.Position = position;

            return Convert.ToBase64String(hash);
        }

        public static void Verify(Stream input, string md5Hash) //TODO should probably be awaited
        {
            if (md5Hash != Hash(input))
            {
                throw new ChannelException(412, "MD5 hash received does not match hash calculated on server. Please resubmit.");
            }
        }
    }
}