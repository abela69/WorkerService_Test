using Microsoft.Extensions.Configuration;
using WorkerService_Test.Services;
using FluentAssertions;



namespace WorkerService_Test.Tests
{
    public class SegmentServiceTests
    {
        private readonly SegmentService _segmentService;

        public SegmentServiceTests()
        {
            // GetConnectionString extension method-ია
            // ამიტომ ConfigurationBuilder-ს ვიყენებთ
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=devcluster\\devserv;Database=BANK2000;Trusted_Connection=True;TrustServerCertificate=True;"
                })
                .Build();

            _segmentService = new SegmentService(config);
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

    }
}
