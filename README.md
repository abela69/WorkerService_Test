# Statistics Worker Service

## რა არის?
Background სერვისი რომელიც RabbitMQ-დან იღებს ტრანზაქციების ივენთებს და SQL Server-ში ითვლის კლიენტების სეგმენტების სტატისტიკას.

## ტექნოლოგიები
- .NET Worker Service
- RabbitMQ (Topic Exchange)
- SQL Server
- Dapper
- Serilog

## არქიტექტურა
```
RabbitMQ (B6.Transactions)
    ├── b6.transaction.create  → +1
    └── b6.transaction.delete  → -1
            ↓
    StatisticsWorker
            ↓
    SegmentService (კლიენტის სეგმენტი)
            ↓
    StatisticsRepository (SQL)
            ↓
    BANK2000.basis.STATISTICS_TEST
```

## პროექტის სტრუქტურა
```
WorkerService_Test/
├── Logging/
│   └── LoggingConfiguration.cs
├── Models/
│   └── DocumentMessage.cs
├── Services/
│   └── SegmentService.cs
├── Repository/
│   └── StatisticsRepository.cs
├── Worker/
│   └── StatisticsWorker.cs
├── Program.cs
└── appsettings.json
```

## კლიენტის სეგმენტები
| სეგმენტი | პირობა |
|---|---|
| Company | იურიდიული პირი |
| Unique | UNIQUE_BANKER ატრიბუტი |
| Premium | PREMIUM_BANKER ატრიბუტი |
| Mass | სხვა |
| N/A | კლიენტი არ ჰყავს |

## წესები
- საბუთის შექმნა → Count +1
- საბუთის წაშლა → Count -1
- თუ ორივე მხარეს კლიენტი არ ჰყავს → იგნორირება
- Count ნულზე ნაკლები არ გახდება
- Delete ბრძანება იგნორირდება თუ row არ არსებობს

## ლოგირება
- გამოიყენება **Serilog**
- ლოგები ინახება: `Logging/logs/log-YYYYMMDD.txt`
- ინახება მაქსიმუმ **50 ფაილი** (ძველი ავტომატურად იშლება)
- Microsoft/System ლოგები გამორიცხულია

## კონფიგურაცია
appsettings.json-ში შეავსე შემდეგი:
```json
{
  "RabbitMQ": {
    "Host": "...",
    "Port": 5672,
    "Username": "...",
    "Password": "...",
    "VirtualHost": "...",
    "Exchange": "..."
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=BANK2000;Trusted_Connection=True;"
  }
}
```

## SQL ცხრილი
```sql
CREATE TABLE BANK2000.basis.STATISTICS_TEST (
    ID             INT IDENTITY(1,1) PRIMARY KEY,
    Debit_Segment  VARCHAR(20),
    Credit_Segment VARCHAR(20),
    ChannelID      INT,
    OP_Date        DATE,
    OP_Count       INT DEFAULT 0,
    CONSTRAINT UQ_Stats UNIQUE (Debit_Segment, Credit_Segment, ChannelID, OP_Date)
);
```
