using QuickFlow.Domain.Common;

namespace QuickFlow.Tests.Common;

public class ValidatorTests
{
    [Fact]
    public void Required_blank_value_throws_validation_exception_for_field()
    {
        var ex = Assert.Throws<DomainValidationException>(() =>
            new Validator().Required("Title", "   ", 10).ThrowIfInvalid());

        Assert.True(ex.Errors.ContainsKey("Title"));
    }

    [Fact]
    public void Required_value_over_max_length_is_invalid()
    {
        var v = new Validator().Required("Title", new string('a', 11), 10);
        Assert.True(v.HasErrors);
    }

    [Fact]
    public void Valid_values_do_not_throw()
    {
        new Validator().Required("Title", "ok", 10).Optional("Description", null, 5).ThrowIfInvalid();
    }
}
