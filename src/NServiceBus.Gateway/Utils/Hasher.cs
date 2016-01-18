namespace NServiceBus.Gateway.Utils
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Receiving;

    class Hasher
    {
        internal static async Task Verify(Stream input, string md5Hash)
        {
            if (md5Hash != await Hash(input).ConfigureAwait(false))
            {
                throw new ChannelException(412, "MD5 hash received does not match hash calculated on server. Please resubmit.");
            }
        }

        internal static async Task<string> Hash(Stream stream)
        {
            var position = stream.Position;
            byte[] hash;
            using (var md5 = MD5.Create())
            {
                hash = await ComputeHashAsync(md5, stream).ConfigureAwait(false);
            }

            stream.Position = position;
            return Convert.ToBase64String(hash);
        }

        private static async Task<byte[]> ComputeHashAsync(HashAlgorithm algorithm, Stream inputStream)
        { 
           const int BufferSize = 4096;

           algorithm.Initialize();

           var buffer = new byte[BufferSize];
           var streamLength = inputStream.Length;
           while (true)
            {
                var read = await inputStream.ReadAsync(buffer, 0, BufferSize).ConfigureAwait(false);
                if (inputStream.Position == streamLength)
                {
                    algorithm.TransformFinalBlock(buffer, 0, read);
                    break;
                }
                algorithm.TransformBlock(buffer, 0, read, default(byte[]), default(int));
            }
            return algorithm.Hash;
        }
    }
}