# REST API

Минимальный API для мобильного клиента (`.NET MAUI`). Используется JWT bearer auth, отдельная схема `Bearer` параллельно с cookie-аутентификацией веба.

База: `https://бокатюк.бел`

## Аутентификация

### `POST /api/auth/login`

Анонимный. Возвращает JWT.

Тело:
```json
{ "email": "user@example.com", "password": "secret123" }
```

Ответ `200`:
```json
{
  "token": "eyJhbGciOi...",
  "expiresAtUtc": "2026-04-30T10:15:00Z",
  "fullName": "Иван Иванов",
  "roles": ["Worker"]
}
```

Ошибки: `401 invalid_credentials` (неверный email/пароль или пользователь деактивирован).

### `GET /api/auth/me`

Требует `Authorization: Bearer <jwt>`. Отдаёт текущего пользователя.

```json
{
  "id": "…",
  "email": "user@example.com",
  "fullName": "Иван Иванов",
  "divisionId": "…",
  "roles": ["Worker"]
}
```

## TimeEntries

Все ручки требуют bearer.

### `GET /api/timeentries?from=YYYY-MM-DD&to=YYYY-MM-DD&workerId=<guid>`

Параметры:
- `from`, `to` — диапазон дат (если не указан — последние 30 дней).
- `workerId` — для менеджера/админа фильтр по воркеру; для воркера игнорируется.

Видимость записей:
- Worker — только свои.
- Manager — только своё подразделение.
- Admin — все.

Ответ `200`:
```json
[
  {
    "id": "…",
    "workerId": "…",
    "workDate": "2026-04-28",
    "hours": 8,
    "taskName": "Доставка груза",
    "carId": "…",
    "carName": "Renault Master",
    "licensePlate": "AB-1234-7",
    "description": "…",
    "createdAt": "2026-04-28T19:00:00Z",
    "updatedAt": null
  }
]
```

### `POST /api/timeentries`

Создать запись. Если `workerId` не указан — берётся текущий пользователь.

Тело:
```json
{
  "workerId": null,
  "workDate": "2026-04-28",
  "hours": 8,
  "taskName": "Доставка груза",
  "carName": "Renault Master",
  "licensePlate": "AB-1234-7",
  "description": "…"
}
```

Валидация — см. [validation.md](./validation.md). Если есть `carName`, должен быть и `licensePlate`, и наоборот.

Ответ `201`:
```json
{ "id": "новый-guid" }
```

Ошибки: `400 work_date_in_future`, `400 car_fields_inconsistent`, `403` (нет прав для целевого воркера).

## Конфигурация JWT (на стороне сервера)

Требуется `Jwt:Key` (минимум 32 символа), `Jwt:Issuer`, `Jwt:Audience`. В Plesk → Application Settings:

```
Jwt:Issuer=korendzh
Jwt:Audience=korendzh-clients
Jwt:Key=<сгенерированный 64-символьный секрет>
Jwt:AccessTokenLifetimeMinutes=1440
```

Если `Jwt:Key` пуст — JWT-аутентификация не подключается, мобильный клиент работать не будет.

---

*Документ обновлён: 2026-04-29*
