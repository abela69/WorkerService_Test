using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using WorkerService_Test.Services;
using FluentAssertions;



namespace WorkerService_Test.Tests
{
    public class SegmentServiceTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly SegmentService _segmentService;

        public SegmentServiceTests()
        {
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c.GetConnectionString("DefaultConnection"))
                .Returns("Server=devcluster\\devserv;Database=BANK2000;Trusted_Connection=True;TrustServerCertificate=True;");

            _segmentService = new SegmentService(_configMock.Object);
        }

        [Fact]
        public async Task GetQuery_WhenClientNotFound_ReturnsNA()
        {
            var result = await _segmentService.GetQuery(0);
            result.Should().Be("N/A");
        }

        [Fact]
        public async Task GetQuery_WhenClientIsCompany_ReturnsCompany()
        {
            var result = await _segmentService.GetQuery(857469);
            result.Should().BeOneOf("Company", "Mass", "Premium", "Unique", "N/A");
        }

        [Fact]
        public async Task GetQuery_WhenClientIsJuridical_ReturnsCompany()
        {
            var result = await _segmentService.GetQuery(106);
            result.Should().Be("Company");
        }

        [Fact]
        public async Task GetQuery_WhenClientIsMass_ReturnsMass()
        {
            var result = await _segmentService.GetQuery(21);
            result.Should().Be("Mass");
        }

        [Fact]
        public async Task GetQuery_WhenClientNotExists_ReturnsNA()
        {
            var result = await _segmentService.GetQuery(0);
            result.Should().Be("N/A");
        }


    }
}
