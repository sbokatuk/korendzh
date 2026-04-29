# korendzh

Система учёта рабочего времени. Веб-приложение (ASP.NET Core 8 + Razor Pages) и (планируемое) мобильное приложение на .NET MAUI.

Документация — в [`/docs`](./docs/README.md). Это README покрывает только сборку и запуск.

## Требования

- .NET SDK 8.0
- SQL Server 2019+ (или LocalDB для разработки)
- Доступ к SMTP-серверу для email (для прод)

## Структура

```
src/
  Korendzh.Domain/          # Доменные сущности (без зависимостей от ASP.NET)
  Korendzh.Infrastructure/  # EF Core, Identity, сервисы (notifications, audit, invites)
  Korendzh.Web/             # ASP.NET Core (Razor Pages + API)
docs/                       # Полная документация проекта
.github/workflows/          # GitHub Actions: сборка → ветка `deploy`
```

## Первый запуск локально

```bash
git clone https://github.com/sbokatuk/korendzh.git
cd korendzh
dotnet restore
```

### 1. Сгенерировать первоначальную миграцию EF Core

Миграции в репозитории не хранятся (генерируются при первой сборке). Выполните один раз:

```bash
dotnet tool install --global dotnet-ef --version 8.*    # если ещё не установлен
dotnet ef migrations add Initial --project src/Korendzh.Infrastructure --startup-project src/Korendzh.Web
```

Файлы миграции лягут в `src/Korendzh.Infrastructure/Migrations/`. После этого закоммитьте их.

### 2. Настроить секреты разработчика

```bash
cd src/Korendzh.Web
dotnet user-secrets set "ConnectionStrings:Default" "Server=(localdb)\\MSSQLLocalDB;Database=KorendzhDev;Trusted_Connection=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Seed:AdminEmail"    "admin@example.com"
dotnet user-secrets set "Seed:AdminPassword" "Pa55word!"
```

(Для прода те же значения задаются через Plesk → Application Settings — см. [docs/deployment.md](./docs/deployment.md).)

### 3. Запустить

```bash
dotnet run --project src/Korendzh.Web
```

При первом старте применятся миграции и создастся админ-аккаунт из переменных `Seed:*`.

## Переменные окружения / конфигурация

| Ключ | Назначение |
|---|---|
| `ConnectionStrings:Default` | Строка подключения к MSSQL |
| `Email:Host`, `Email:Port`, `Email:User`, `Email:Password`, `Email:UseStartTls` | SMTP для рассылок |
| `Email:FromAddress`, `Email:FromName`, `Email:AppBaseUrl` | Параметры отправителя и базовый URL для ссылок в письмах |
| `Google:ClientId`, `Google:ClientSecret` | Google OAuth (опционально) |
| `Seed:AdminEmail`, `Seed:AdminPassword`, `Seed:AdminFullName` | Параметры первичного админ-аккаунта |

Полное описание стэка и инфраструктуры — [docs/requirements.md](./docs/requirements.md), [docs/deployment.md](./docs/deployment.md).

## Деплой

Деплой ведётся через ветку `deploy`, в которую GitHub Actions пушат собранный артефакт. Plesk на `w14.hoster.by` слушает эту ветку и обновляет `\httpdocs` автоматически.

Запуск сборки:
- автоматически — пуш тега `v*` в `main`;
- вручную — Actions → Build and publish to deploy branch → Run workflow.

См. [docs/deployment.md](./docs/deployment.md) для пошагового чеклиста.

## Что уже реализовано (skeleton + ядро)

- Доменные сущности, EF Core, Identity (Admin/Manager/Worker)
- Авторизация (политики Admin/Manager+Admin), DivisionScope для проверок
- Email-инвайты для воркеров и менеджеров
- Self-service сброс пароля + принудительный сброс менеджером/админом
- TimeEntry CRUD с soft-delete и optimistic concurrency
- Справочник автомобилей с автокомплитом (создание из формы)
- Управление воркерами (менеджер/админ), менеджерами и подразделениями (админ)
- Статистика по воркерам / задачам / автомобилям + CSV-экспорт
- Audit log через EF SaveChangesInterceptor
- Очередь email-уведомлений с ретраями (фоновый сервис)
- Push-отправитель — заглушка для следующих итераций (APNS/FCM)
- GitHub Actions workflow для деплоя

## Что планируется

- .NET MAUI мобильное приложение (см. [docs/system-overview.md](./docs/system-overview.md))
- APNS / FCM реализация push
- Excel (xlsx) экспорт статистики (сейчас только CSV)
- Визуализация графиков для админ-статистики
- Полный Google OAuth-флоу (сейчас задел в Program.cs)
- Покрытие тестами

## Документация

Полная документация — в папке [`/docs`](./docs/README.md):

- [system-overview.md](./docs/system-overview.md)
- [user-stories.md](./docs/user-stories.md)
- [roles-permissions.md](./docs/roles-permissions.md)
- [data-model.md](./docs/data-model.md)
- [validation.md](./docs/validation.md)
- [notifications.md](./docs/notifications.md)
- [requirements.md](./docs/requirements.md)
- [deployment.md](./docs/deployment.md)
