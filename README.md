# ParseContentApi

Endpoint HTTP (ASP.NET Core Minimal API, .NET 10) implementujący `POST /api/v1/parse-content` —
przyjmuje ładunek JSON z danymi zakodowanymi w Base64 (CSV lub INTERNAL_JSON), parsuje je
i zwraca ujednoliconą odpowiedź JSON.

## Wymagania

- .NET SDK 8.0 lub nowszy (projekt skonfigurowany pod .NET 10.0)

## Uruchomienie lokalne

```bash
git clone https://github.com/maaniek697/parse-content-api.git
cd parse-content-api/ParseContentApi
dotnet restore
dotnet run
```

Po uruchomieniu aplikacja nasłuchuje domyślnie pod adresem podanym w konsoli, np.: