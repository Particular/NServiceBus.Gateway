namespace NServiceBus.Core.Tests
{
    using System.Threading.Tasks;
    using Gateway.Receiving;
    using Gateway.Utils;
    using NUnit.Framework;

    [TestFixture]
    public class HasherTests
    {

        [Test]
        public async void Valid_Md5_can_be_verified()
        {
            await Hasher.Verify("myData".ConvertToStream(), "4HJGsZlkhfKtZTbdlkaTgw==");
        }

        [Test]
        [ExpectedException(typeof(ChannelException))]
        public async void Invalid_hash_throws_ChannelException()
        {
             await Hasher.Verify("myData".ConvertToStream(), "invalidHash");
        }
    }
}