using FluentAssertions;
using WorkerService_Test.Repository;
using Microsoft.Extensions.Configuration;


namespace WorkerService_Test.Tests
{
    public class StatisticsRepositoryTests
    {
        private readonly StatisticsRepository _repository;

        public StatisticsRepositoryTests()
        {
            // GetConnectionString extension method-ია
            // ამიტომ Mock-ის მაგივრად პირდაპირ IConfiguration-ს ვაწყობთ
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=devcluster\\devserv;Database=BANK2000;Trusted_Connection=True;TrustServerCertificate=True;"
                })
                .Build();

            _repository = new StatisticsRepository(config);
        }

        [Fact]
        public async Task ExistsAsync_WhenRowNotExists_ReturnsFalse()
        {
            var result = await _repository.ExistsAsync("N/A", "N/A", 0, DateTime.MinValue);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateStatisticsAsync_WhenCalled_ShouldNotThrow()
        {
            var action = async () => await _repository.UpdateStatisticsAsync(
                "Mass", "Company", 1, DateTime.Today, 1
            );
            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ExistsAsync_AfterInsert_ReturnsTrue()
        {
            await _repository.UpdateStatisticsAsync("Mass", "Mass", 99, DateTime.Today, 1);
            var result = await _repository.ExistsAsync("Mass", "Mass", 99, DateTime.Today);
            result.Should().BeTrue();
        }
    }
}
