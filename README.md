## Przykładowe żądanie

```bash
curl -X POST http://localhost:5199/api/v1/parse-content \
  -H "Content-Type: application/json" \
  -d '{
        "type": "CSV",
        "content": "aWQsbmFtZSxwcmljZQoxLEthd2EsMTIuNTAKMixIZXJiYXRhLDkuOTAK"
      }'
```

Zwraca:

```json
{
  "status": "success",
  "type": "CSV",
  "processedCount": 2,
  "data": [
    { "id": "1", "name": "Kawa", "price": "12.50" },
    { "id": "2", "name": "Herbata", "price": "9.90" }
  ]
}
```

Analogicznie działa dla `"type": "INTERNAL_JSON"` (zdekodowana zawartość musi być tablicą
obiektów JSON, np. `[{"id":1,"name":"Kawa"}]`).

## Obsługa błędów

Nieprawidłowy/nieobsługiwany `type`, niepoprawny Base64 lub błędna struktura danych
skutkują odpowiedzią `400 Bad Request` z ujednoliconą strukturą:

```json
{ "status": "error", "error": "...", "detail": "..." }
```
