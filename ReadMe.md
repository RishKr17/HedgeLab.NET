# HedgeLab.NET (Day 1)

Fixed-income curve primitives in C#/.NET 9:
- Nelson–Siegel–Svensson zero curve (`ZeroRate(t)`)
- Discount factors (`DiscountFactor(t)`), continuous compounding
- Unit tests (xUnit)

## Build & Test
```bash
dotnet restore
dotnet build -c Release
dotnet test