# SpaceHub

სივრცეების დაჯავშნისა და ღონისძიებების მართვის სასწავლო პროექტი, ქართულენოვანი ვებინტერფეისით.

მფლობელი ამატებს სივრცეს, სამუშაო საათებს, მომსახურებებსა და პაკეტებს. მომხმარებელი აგზავნის ჯავშნის მოთხოვნას, რომელსაც მფლობელი ადასტურებს ან უარყოფს. ორგანიზატორი საკუთარ დადასტურებულ ჯავშანზე ქმნის ღონისძიებას, ხოლო მონაწილე რეგისტრირდება და იღებს ბილეთს.

## ტექნოლოგიები

C#, ASP.NET Core (.NET 10), Entity Framework Core, SQL Server, JWT, FluentValidation, AutoMapper, React და TypeScript.

## სტრუქტურა

- `src/SpaceHub.Domain` — მონაცემების მოდელები და სტატუსები.
- `src/SpaceHub.Application` — ბიზნესლოგიკა, DTO-ები და ვალიდაცია.
- `src/SpaceHub.Infrastructure` — ბაზასთან მუშაობა და ავტორიზაციის ტექნიკური ნაწილი.
- `src/SpaceHub.Api` — კონტროლერები და API-ის კონფიგურაცია.
- `frontend` — ვებინტერფეისი.
- `tests` — ავტომატური ტესტები.

ღონისძიებებისა და ბილეთების მართვის სრული ფუნქციები ხელმისაწვდომია API/Swagger-ში; ვებინტერფეისი მოიცავს ანგარიშებს, სივრცეებს, კატალოგსა და ჯავშნებს.

## გაშვება

საჭიროა .NET 10 SDK, Node.js 22.12+ და SQL Server Express LocalDB. ქვემოთ მოცემული ბრძანებები გაუშვი Windows PowerShell-ში, პროექტის საქაღალდიდან:

```powershell
dotnet restore SpaceHub.slnx -m:1
.\scripts\Initialize-Development.ps1
.\scripts\Build-Frontend.ps1
.\scripts\Seed-DemoData.ps1
dotnet run --project .\src\SpaceHub.Api
```

პირველი სკრიპტი ქმნის ლოკალურ JWT გასაღებს. დემომონაცემების სკრიპტი იყენებს მიგრაციებს და ამზადებს `SpaceHubSimple` ბაზას. ლოკალური პარამეტრები Git-ში არ იტვირთება.

- საიტი: http://localhost:53425
- Swagger: http://localhost:53425/swagger

დემო ანგარიშები: `owner.demo@spacehub.example`, `organizer.demo@spacehub.example`, `attendee.demo@spacehub.example`.

სამივეს სასწავლო პაროლია `SpaceHubDemo!2026`. დემო იმპორტი მხოლოდ Development რეჟიმში მუშაობს.

## ტესტირება

გააჩერე გაშვებული API და გაუშვი:

```powershell
.\scripts\Verify-Project.ps1
```

ინტეგრაციულ ტესტებს სჭირდება მოქმედი LocalDB. ისინი იყენებს ცალკე დროებით ბაზებს.
