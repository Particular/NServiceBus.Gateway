namespace NServiceBus.Core.Tests
{
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
        public void Invalid_hash_throws_ChannelException()
        {
            Assert.That(async () => { await Hasher.Verify("myData".ConvertToStream(), "invalidHash"); }, Throws.Exception.TypeOf<ChannelException>());
        }
    }
}