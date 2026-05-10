using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.SqlValidator.WhenWorkingWithTheSqlValidator;

public class SqlValidatorTestBase
{
    protected Pokepad.Gold.Api.Middleware.SqlValidator SqlValidator { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.SqlValidator = new Pokepad.Gold.Api.Middleware.SqlValidator();
    }

    [SetUp]
    public virtual void SetUp()
    {
    }
}
