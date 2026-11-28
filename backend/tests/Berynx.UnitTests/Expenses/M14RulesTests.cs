using Berynx.Domain.Expenses;
using NUnit.Framework;

namespace Berynx.UnitTests.Expenses;

[TestFixture]
public sealed class M14RulesTests
{
    [TestCase("Direct", true)]
    [TestCase("Indirect", true)]
    [TestCase("Capital", true)]
    [TestCase("Other income", true)]
    [TestCase("Payroll", false)]
    public void Category_types_are_explicit(string type, bool expected) =>
        Assert.That(M14Rules.IsCategoryType(type), Is.EqualTo(expected));

    [Test]
    public void Only_approved_expense_can_post()
    {
        var expense = new M14Expense { Status = "Pending approval", Amount = 100m };
        Assert.Throws<InvalidOperationException>(() => M14Rules.RequirePostable(expense));
    }
}
