using Core.Reports;

namespace Core.Tests;

public class ReportServiceTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(5, 3)] // читачів лише троє
    public void TopReaders_ReturnsAtMostN(int n, int expected) =>
        Assert.Equal(expected, ReportService.TopReaders(TestData.Loans(), n).Count);

    [Fact] // детермінований топ-N
    public void TopReaders_IsDeterministic() =>
        Assert.Equal(new[] { "R1", "R2" },
            ReportService.TopReaders(TestData.Loans(), 2).Select(r => r.ReaderId));

    [Fact] // термін 14 днів: усі видачі у даних свіжіші
    public void OverdueLoans_FreshLoans_ReturnsEmpty() =>
        Assert.Empty(ReportService.OverdueLoans(TestData.Loans(), TestData.Today));

    [Fact]
    public void OverdueLoans_OldOpenLoan_IsReported()
    {
        var old = Core.Domain.Loan.FromDto(new Core.Dto.LoanDto(
            "L-old", "C-1", "R9", TestData.Today.AddDays(-20), null));

        Assert.Single(ReportService.OverdueLoans([old], TestData.Today));
    }
}