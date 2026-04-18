using FluentAssertions;
using Xunit;

namespace Tharga.Mcp.Tests.Contracts;

public class McpScopeTests
{
    [Fact]
    public void Scope_has_three_levels()
    {
        Enum.GetValues<McpScope>().Should().BeEquivalentTo(new[]
        {
            McpScope.User,
            McpScope.Team,
            McpScope.System,
        });
    }
}
