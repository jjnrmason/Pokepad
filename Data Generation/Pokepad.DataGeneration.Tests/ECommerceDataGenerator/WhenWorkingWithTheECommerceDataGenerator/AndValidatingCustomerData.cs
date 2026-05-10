using NUnit.Framework;

namespace Pokepad.DataGeneration.Tests.ECommerceDataGenerator.WhenWorkingWithTheECommerceDataGenerator;

public partial class WhenWorkingWithTheECommerceDataGenerator
{
    public class AndValidatingCustomerData : ECommerceDataGeneratorTestBase
    {
        [Test]
        public void ThenAllCustomersHaveANonEmptyFirstName()
        {
            Assert.That(this.Result.Customers.All(c => !string.IsNullOrWhiteSpace(c.FirstName)), Is.True);
        }

        [Test]
        public void ThenAllCustomersHaveANonEmptyLastName()
        {
            Assert.That(this.Result.Customers.All(c => !string.IsNullOrWhiteSpace(c.LastName)), Is.True);
        }

        [Test]
        public void ThenAllCustomersHaveANonEmptyEmail()
        {
            Assert.That(this.Result.Customers.All(c => !string.IsNullOrWhiteSpace(c.Email)), Is.True);
        }

        [Test]
        public void ThenAllCustomersHaveANonEmptyPhone()
        {
            Assert.That(this.Result.Customers.All(c => !string.IsNullOrWhiteSpace(c.Phone)), Is.True);
        }

        [Test]
        public void ThenAllCustomersHaveACreatedAtDateInThePast()
        {
            Assert.That(this.Result.Customers.All(c => c.CreatedAt < DateTime.Now), Is.True);
        }

        [Test]
        public void ThenAllCustomerIdsAreValidGuids()
        {
            Assert.That(this.Result.Customers.All(c => Guid.TryParse(c.CustomerId, out _)), Is.True);
        }
    }
}
