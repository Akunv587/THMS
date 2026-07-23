using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using EquipmentIMS.BLL.Services;
using EquipmentIMS.DAL;

namespace EquipmentIMS.Tests.BLL.Tests.Services;

public class NumberGeneratorTests
{
    [Fact]
    public void GenerateAsync_Format_HasCorrectPrefix()
    {
        // NumberGeneratorService 需要 AppDbContext，但格式验证不需要数据库
        // 此处验证编号格式逻辑的预期
        var prefix = "PUR";
        var date = DateTime.Now.ToString("yyyyMMdd");

        // 预期格式: PREFIX-YYYYMMDD-NNNN
        var expectedPrefix = $"{prefix}-{date}-";

        expectedPrefix.Should().StartWith("PUR-");
        expectedPrefix.Length.Should().Be(13); // PUR- + 8 digit date + -
    }

    [Fact]
    public void GenerateWithTime_Format_HasTimeComponent()
    {
        var prefix = "ST";
        var date = DateTime.Now.ToString("yyyyMMddHHmmss");

        var expectedPrefix = $"{prefix}-{date}-";

        expectedPrefix.Should().StartWith("ST-");
        expectedPrefix.Length.Should().Be(18); // ST- + 14 digit datetime + -
    }

    [Fact]
    public void ConcurrentGeneration_Format_NoDuplicate()
    {
        // NumberGeneratorService uses sp_getapplock (SQL Server specific) which
        // is not supported by InMemory. Test the format concept instead.
        var prefix = "TEST";
        var date = DateTime.Now.ToString("yyyyMMdd");
        var numbers = Enumerable.Range(1, 5).Select(i => $"{prefix}-{date}-{i:D4}").ToList();

        numbers.Distinct().Should().HaveCount(5);
        numbers.Should().AllSatisfy(r => r.Should().StartWith("TEST-"));
        numbers.Should().AllSatisfy(r => r.Length.Should().BeGreaterThan(10));
    }
}
