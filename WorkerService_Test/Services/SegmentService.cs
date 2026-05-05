using Dapper;
using Microsoft.Data.SqlClient;

namespace WorkerService_Test.Services
{
    public class SegmentService
    {
        private readonly string _connectionString;

        public SegmentService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }



        public async Task<string> GetQuery(int clientId)
        {
            using var connection = new SqlConnection(_connectionString);

            var isCompany = await connection.QueryFirstOrDefaultAsync<int?>(
                @"SELECT IS_JURIDICAL FROM BANK2000.dbo.CLIENTS 
                WHERE CLIENT_NO = @ClientId",
                new { ClientId = clientId }
            );

            if (isCompany == null) return "N/A";
            if (isCompany == 1) return "Company";

            var result = await connection.QueryFirstOrDefaultAsync<string>(
                @"SELECT CASE 
            WHEN ATTRIB_CODE = 'UNIQUE_BANKER'  THEN 'Unique'
            WHEN ATTRIB_CODE = 'PREMIUM_BANKER' THEN 'Premium'
            ELSE 'Mass'
            END
            FROM BANK2000.dbo.CLIENT_ATTRIBUTES
            WHERE CLIENT_NO = @ClientId
            AND ATTRIB_CODE IN ('UNIQUE_BANKER', 'PREMIUM_BANKER')",
                new { ClientId = clientId }
            );

            return result ?? "Mass";
        }
    }
}
