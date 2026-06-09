using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using WorkerService_Test.Repository;
using Microsoft.Extensions.Configuration;


namespace WorkerService_Test.Tests
{
    public class StatisticsRepositoryTests
    {
        private readonly StatisticsRepository _repository;

        public StatisticsRepositoryTests()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c.GetConnectionString("DefaultConnection"))
                .Returns("Server=devcluster\\devserv;Database=BANK2000;Trusted_Connection=True;TrustServerCertificate=True;");

            _repository = new StatisticsRepository(configMock.Object);
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
