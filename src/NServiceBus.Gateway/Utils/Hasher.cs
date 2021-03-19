namespace NServiceBus.Gateway.Utils
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Receiving;

    class Hasher
    {
        public static async Task Verify(Stream input, string md5Hash, CancellationToken cancellationToken = default)
        {
            if (md5Hash != await Hash(input, cancellationToken).ConfigureAwait(false))
            {
                throw new ChannelException(412, "MD5 hash received does not match hash calculated on server. Please resubmit.");
            }
        }

        public static async Task<string> Hash(Stream stream, CancellationToken cancellationToken = default)
        {
            var position = stream.Position;
            byte[] hash;
            using (var md5 = MD5.Create())
            {
                hash = await ComputeHashAsync(md5, stream, cancellationToken).ConfigureAwait(false);
            }

            stream.Position = position;
            return Convert.ToBase64String(hash);
        }

        static async Task<byte[]> ComputeHashAsync(HashAlgorithm algorithm, Stream inputStream, CancellationToken cancellationToken)
        {
            const int BufferSize = 4096;

            algorithm.Initialize();

            var buffer = new byte[BufferSize];
            var streamLength = inputStream.Length;
            while (true)
            {
                var read = await inputStream.ReadAsync(buffer, 0, BufferSize, cancellationToken).ConfigureAwait(false);
                if (inputStream.Position == streamLength)
                {
                    algorithm.TransformFinalBlock(buffer, 0, read);
                    break;
                }
                algorithm.TransformBlock(buffer, 0, read, default, default);
            }
            return algorithm.Hash;
        }
    }
}