using Dapper;
using Microsoft.Data.SqlClient;

namespace WorkerService_Test.Repository
{
    public class StatisticsRepository
    {

        private readonly string _connectionString;

        public StatisticsRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }


        public async Task<bool> ExistsAsync(string debitSegment,string creditSegment,int channelId,DateTime date)
        {
            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<int>(
                @"SELECT COUNT(1) FROM BANK2000.basis.STATISTICS_TEST
                        WHERE Debit_Segment = @DebitSegment
                          AND Credit_Segment = @CreditSegment
                          AND ChannelID  = @ChannelId
                           AND OP_Date  = @Date",
                new
                {
                    DebitSegment = debitSegment,
                    CreditSegment = creditSegment,
                    ChannelId = channelId,
                    Date = date.Date
                });

            return result > 0;
        }
        public async Task UpdateStatisticsAsync(
            string debitSegment,
            string creditSegment,
            int channelId,
            DateTime date,
            int count)
        {
            using var connection = new SqlConnection(_connectionString);

            await connection.ExecuteAsync(@"
                MERGE BANK2000.basis.STATISTICS_TEST AS target
                USING (SELECT @DebitSegment, @CreditSegment, @ChannelId, @Date)
                      AS source (Debit_Segment, Credit_Segment, ChannelID, OP_Date)
                ON target.Debit_Segment  = source.Debit_Segment
                AND target.Credit_Segment = source.Credit_Segment
                AND target.ChannelID      = source.ChannelID
                AND target.OP_Date        = source.OP_Date
                WHEN MATCHED THEN
                 UPDATE SET OP_Count = CASE 
                   WHEN OP_Count + @Count < 0 THEN 0
                   ELSE OP_Count + @Count 
                   END
                WHEN NOT MATCHED THEN
                    INSERT (Debit_Segment, Credit_Segment, ChannelID, OP_Date, OP_Count)
                    VALUES (@DebitSegment, @CreditSegment, @ChannelId, @Date, @Count);",
               new
               {
                   DebitSegment = debitSegment,
                   CreditSegment = creditSegment,
                   ChannelId = channelId,
                   Date = date.Date,
                   Count = count
               });
        }

    }
}
