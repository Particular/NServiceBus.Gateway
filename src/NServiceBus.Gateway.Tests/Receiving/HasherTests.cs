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
        public Task Valid_Md5_can_be_verified()
        {
            return Hasher.Verify("myData".ConvertToStream(), "4HJGsZlkhfKtZTbdlkaTgw==");
        }

        [Test]
        public void Invalid_hash_throws_ChannelException()
        {
            Assert.That(async () => { await Hasher.Verify("myData".ConvertToStream(), "invalidHash"); }, Throws.Exception.TypeOf<ChannelException>());
        }
    }
}