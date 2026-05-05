# Statistics Worker Service 🏦

## Overview
Background სერვისი რომელიც აკვირდება  ტრანზაქციებს და აგროვებს სტატისტიკას კლიენტების სეგმენტების მიხედვით. სერვისი RabbitMQ-დან იღებს ივენთებს და SQL Server-ში ახახლებს სტატისტიკის ცხრილს.

---

## Tech Stack
| ტექნოლოგია | გამოყენება |
|---|---|
| **.NET Worker Service** | Background სერვისი |
| **RabbitMQ** | Message Broker (Topic Exchange) |
| **SQL Server** | მონაცემთა ბაზა |
| **Dapper** | SQL ORM |
| **Serilog** | ლოგირება |
| **xUnit + FluentAssertions** | Unit Tests |

---

## არქიტექტურა
```
RabbitMQ (B6.Transactions)
    ├── b6.transaction.create  → Count +1
    └── b6.transaction.delete  → Count -1
            ↓
    StatisticsWorker (Background Service)
            ↓
    SegmentService → კლიენტის სეგმენტის განსაზღვრა
            ↓
    StatisticsRepository → SQL MERGE
            ↓
    BANK2000.basis.STATISTICS_TEST
```

---

## პროექტის სტრუქტურა
```
WorkerService_Test/
├── Logging/
│   └── LoggingConfiguration.cs   # Serilog კონფიგურაცია
├── Models/
│   └── DocumentMessage.cs        # RabbitMQ message მოდელი
├── Services/
│   └── SegmentService.cs         # კლიენტის სეგმენტის განსაზღვრა
├── Repository/
│   └── StatisticsRepository.cs   # SQL ოპერაციები
├── Worker/
│   └── StatisticsWorker.cs       # RabbitMQ listener
├── sql/
│   └── WorkerService_Test.sql    # SQL სკრიპტი
├── Program.cs
└── appsettings.json

WorkerService_Test.Tests/
├── SegmentServiceTests.cs        # SegmentService Unit Tests
└── StatisticsRepositoryTests.cs  # StatisticsRepository Unit Tests
```

---

## კლიენტის სეგმენტები
სეგმენტი განისაზღვრება შემდეგი პრიორიტეტით:

| პრიორიტეტი | სეგმენტი | პირობა |
|---|---|---|
| 1 | **N/A** | კლიენტი არ ჰყავს ანგარიშს |
| 2 | **Company** | იურიდიული პირი |
| 3 | **Unique** | UNIQUE_BANKER ატრიბუტი |
| 4 | **Premium** | PREMIUM_BANKER ატრიბუტი |
| 5 | **Mass** | სხვა შემთხვევა |

---

## ბიზნეს წესები
- ✅ საბუთის შექმნა → Count **+1**
- ✅ საბუთის წაშლა → Count **-1**
- ✅ თუ ორივე მხარეს კლიენტი არ ჰყავს → **იგნორირება**
- ✅ Count **ნულზე ნაკლები არ გახდება**
- ✅ Delete იგნორირდება თუ **row არ არსებობს**

---

## ლოგირება
- გამოიყენება **Serilog**
- ლოგები ინახება: `Logging/logs/log-YYYYMMDD.txt`
- ინახება მაქსიმუმ **50 ფაილი**
- ძველი ფაილი ავტომატურად იშლება
- Microsoft/System ლოგები გამორიცხულია

---

## კონფიგურაცია
`appsettings.json`:
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
    "DefaultConnection": "Server=...;Database=BANK2000;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

---

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

---

## როგორ გაუშვა
1. `appsettings.json` შეავსე კონფიგურაციით
2. SQL ცხრილი შექმენი
3. გაუშვი:
```bash
dotnet run
```

---

## Unit Tests გაშვება
```bash
dotnet test
```
