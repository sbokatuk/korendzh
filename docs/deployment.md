# Деплоймент

Документ описывает разворачивание системы (веб + БД) на хостинге **hoster.by** под управлением **Plesk** (Windows Server 2025 + IIS 10.0). Мобильное приложение — отдельный канал дистрибуции (App Store / Google Play).

Связано с [requirements.md](./requirements.md) (стек), [data-model.md](./data-model.md) (сидинг), [system-overview.md](./system-overview.md) (безопасность).

> **Важно про .NET 8 на hoster.by.** Поддержка ASP.NET Core / .NET 8 на сервере есть, но **версия указывается в файлах сайта**, а не в Plesk-панели «Конфигурация ASP.NET» (там доступны только классические Framework 3.5/4.8 — она к нашему приложению отношения не имеет). Версия .NET 8 определяется через `web.config` (`aspNetCore` handler) и `Korendzh.Web.runtimeconfig.json` — оба файла попадают в `\httpdocs` автоматически из публикации.

## Реальные имена проектов и путей

В коде:

- Решение: `Korendzh.sln` (в корне репозитория).
- Проекты: `src/Korendzh.Domain`, `src/Korendzh.Infrastructure`, `src/Korendzh.Web`.
- Главный артефакт: `Korendzh.Web.dll` (его прописывает `web.config` в `aspNetCore arguments`).
- DbContext-сборка для миграций: `Korendzh.Infrastructure.dll`.
- Workflow: `.github/workflows/deploy.yml` (триггеры — пуш тега `v*` или ручной запуск).

После сборки `dotnet publish src/Korendzh.Web/Korendzh.Web.csproj -c Release` папка `publish/` содержит всё, что нужно положить в `\httpdocs`.

## Целевая инфраструктура

| Параметр | Значение |
|---|---|
| Хостинг-провайдер | hoster.by |
| Хост | `w14.hoster.by` |
| Панель управления | Plesk |
| ОС | Windows Server 2025 |
| Веб-сервер | IIS 10.0 (через Plesk) |
| Корень сайта | `\httpdocs` |
| Домен | `бокатюк.бел` (Punycode: `xn--80aaaifc7a8azal.xn--90ais`) |

## Репозиторий и режим деплоя

- **Источник кода:** GitHub, репозиторий `sbokatuk/korendzh` (`https://github.com/sbokatuk/korendzh`).
- **Тип репозитория в Plesk:** «Удалённый репозиторий» — Plesk **получает** код по push-сигналу из GitHub.
- **Режим:** автоматическое развёртывание. На каждый push в целевую ветку Plesk обновляет `\httpdocs`.
- **Целевая папка:** `\httpdocs` (см. скриншот настройки).

### Стратегия веток

Чтобы не пушить исходники в продакшн, используется отдельная **ветка артефактов**:

- `main` — исходники, разработка идёт здесь, Plesk **не подписан** на эту ветку.
- `deploy` (или `release/prod`) — содержит уже собранный `dotnet publish` артефакт. Plesk подписан на эту ветку и кладёт её содержимое прямо в `\httpdocs`.

Ветка `deploy` обновляется через **GitHub Actions** при пуше тега или вручную (workflow_dispatch). Преимущества по сравнению со сборкой на сервере:

- сервер не держит .NET SDK и инструменты сборки;
- неудачная сборка не ломает прод (артефакт пушится в `deploy` только после успешного билда);
- быстрый rollback — `git revert` в `deploy` либо переключение на предыдущий тег.

### Альтернатива: server-side build

Если SDK на сервере доступен и приемлем по нагрузке, в Plesk можно включить «Дополнительные действия развертывания» и прописать:

```
dotnet publish src/Korendzh.Web/Korendzh.Web.csproj -c Release -o publish
```

Затем настроить, чтобы IIS-приложение указывало на папку `publish`. Этот вариант проще, но менее надёжен — зависит от состояния SDK на сервере и удлиняет деплой.

## Подготовка ASP.NET Core под IIS

1. **Microsoft .NET Hosting Bundle** установлен на сервере (выбирается версия под целевой `.NET 8/9/10`). Без него ANCM не запустит приложение.
2. **`web.config`** — генерируется при `dotnet publish`. Должен содержать:
   - `aspNetCore` handler;
   - `processPath="dotnet"`, `arguments=".\Korendzh.Web.dll"`;
   - `hostingModel="InProcess"` (производительность) либо `OutOfProcess` (при необходимости долгих запросов и WebSocket-нагрузки);
   - `stdoutLogEnabled="false"` в проде, `true` — только при разовой диагностике.
3. **App Pool** в Plesk: «No Managed Code» (для .NET Core), пользователь — IIS AppPool, права на чтение `\httpdocs` и запись в `App_Data` / `logs`.

## Конфигурация и секреты

Хранение по слоям, **никогда не коммитим продакшен-секреты в Git**:

| Что | Где |
|---|---|
| Несекретные дефолты | `appsettings.json` (в репо) |
| Несекретные прод-настройки | `appsettings.Production.json` (в репо) |
| **Прод-секреты** (DB, SMTP, Google OAuth, JWT key, Seed admin) | **`appsettings.Local.json` в `\httpdocs`, создаётся вручную в Plesk File Manager** |
| Локальные секреты разработчика | .NET User Secrets (только dev-машина) |

### Почему `appsettings.Local.json`, а не Plesk env-переменные

В тарифе hoster.by (Plesk Windows) нет UI для управления переменными окружения ASP.NET Core. Можно положить `<environmentVariables>` в `web.config`, но он перезаписывается на каждом деплое из ветки `deploy`.

Решение: загружать дополнительный JSON-файл в `Program.cs`:

```csharp
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
```

Файл лежит в `\httpdocs\appsettings.Local.json`, **создаётся вручную через Plesk File Manager**, в репозиторий **не коммитится** (исключён в `.gitignore`). Plesk Git pull не трогает untracked-файлы — `appsettings.Local.json` переживает деплой.

Шаблон `appsettings.Local.json` для копирования (в Plesk File Manager → `httpdocs` → Создать файл `appsettings.Local.json`):

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=Korendzh;User Id=...;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Email": {
    "Host": "smtp.hoster.by",
    "Port": 587,
    "UseStartTls": true,
    "User": "noreply@бокатюк.бел",
    "Password": "<smtp-pass>",
    "FromAddress": "noreply@бокатюк.бел",
    "FromName": "Korendzh",
    "AppBaseUrl": "https://бокатюк.бел"
  },
  "Google": {
    "ClientId": "<optional>",
    "ClientSecret": "<optional>"
  },
  "Jwt": {
    "Key": "<минимум-32-случайных-символа>"
  },
  "Seed": {
    "AdminEmail": "admin@бокатюк.бел",
    "AdminPassword": "<пароль-админа>",
    "AdminFullName": "Сергей Бокатюк"
  }
}
```

Сразу после создания **смените права** на файл, чтобы только AppPool identity мог его читать (Plesk → Permissions).

В качестве альтернативы переменные окружения процесса работают тоже (если Plesk их пробрасывает в IIS App Pool):

- `ConnectionStrings__Default`
- `Email__Host`, `Email__Port`, `Email__User`, `Email__Password`, `Email__UseStartTls`, `Email__FromAddress`, `Email__FromName`, `Email__AppBaseUrl`
- `Google__ClientId`, `Google__ClientSecret`
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key`, `Jwt__AccessTokenLifetimeMinutes`
- `Seed__AdminEmail`, `Seed__AdminPassword`, `Seed__AdminFullName`
- `Push__Apns__KeyId`, `Push__Fcm__ServerKey` — после реализации реальных Push-провайдеров

## База данных (MSSQL)

- **СУБД:** Microsoft SQL Server 2019 (см. [requirements.md](./requirements.md)).
- **Где живёт:** уточняется в тарифе hoster.by. Возможные варианты — встроенный MSSQL Plesk-плана, отдельный managed-инстанс, либо собственный SQL Server. Для прод-нагрузки рекомендуется не Express (ограничение 10 ГБ).
- **Создание БД:** через Plesk → Databases → Add Database. Имя БД, пользователь и пароль фиксируются в Application Settings (см. выше).
- **Сетевой доступ:** только с веб-сервера; внешний доступ закрыт. Для административных задач — RDP/Plesk либо временный whitelist.

### Миграции схемы

Используется **EF Core Migrations**. Стратегия — **migrate-on-startup** с фолбэком на `EnsureCreated()` для бутстрапа.

**Логика в Program.cs:**

```csharp
var hasMigrations = db.Database.GetMigrations().Any();
if (hasMigrations)
    await db.Database.MigrateAsync();   // обычный путь
else
    await db.Database.EnsureCreatedAsync(); // бутстрап без миграций
```

**Два режима:**

1. **Production (рекомендуемый):** в репозитории есть хотя бы одна миграция (`Initial`). На старте `MigrateAsync` применяет все pending миграции. Это работает и для первого деплоя, и для последующих.
2. **Bootstrap (для быстрого старта без `dotnet ef`):** если в коде нет ни одной миграции, EF создаёт схему напрямую через `EnsureCreated()`. Полезно, чтобы развернуть проект «здесь и сейчас», не возясь с CLI. **Caveat:** позже, когда добавите первую миграцию, EF попытается выполнить её на уже существующих таблицах и упадёт. В этот момент нужно либо:
   - очистить БД (drop + create) и пересоздать через миграцию, либо
   - вручную добавить запись в `__EFMigrationsHistory`, чтобы EF считал миграцию уже применённой.

Если потребуется большая миграция (изменение типа колонки на огромной таблице, переиндексирование) — выкатывается отдельным шагом через `dotnet ef migrations bundle` и SQL-скрипт, выполняемый в окно обслуживания.

**Подробный воркфлоу работы с миграциями (генерация, применение, откаты, переход с EnsureCreated)** — см. [migrations.md](./migrations.md).

### Сидинг первого админа

Реализовано в `src/Korendzh.Web/Seeding/DataSeeder.cs`. При старте приложения:

1. Создаются роли `Admin`, `Manager`, `Worker`, если их нет.
2. Если в БД нет ни одного пользователя с ролью `Admin`, создаётся аккаунт по переменным окружения `Seed:AdminEmail`, `Seed:AdminPassword`, `Seed:AdminFullName`.
3. Если переменные не заданы — сидинг пропускается с предупреждением в логах.

После первого входа администратор может сразу сбросить себе пароль через стандартный флоу.

## Домен и SSL

- **Домен:** `бокатюк.бел`.
- **DNS:** A-запись на IP сервера hoster.by. У некоторых регистраторов `.бел` нужно добавлять Punycode (`xn--80aaaifc7a8azal.xn--90ais`).
- **HTTPS:** Plesk → SSL/TLS Certificates → **Let's Encrypt**. ACME-клиент Plesk корректно работает с IDN — выдаёт сертификат на Punycode-имя, IIS отдаёт его и для Cyrillic-варианта.
- **Принудительный HTTPS:** включить через Plesk («Permanent SEO-safe 301 redirect») либо через `URL Rewrite` в `web.config`.
- **HSTS:** включить заголовок `Strict-Transport-Security` после успешной выдачи сертификата.

## Email и push

- **SMTP для транзакционных писем** (инвайты, сброс пароля, нотификации): внешний провайдер (например, hoster.by SMTP с лимитами, либо специализированный сервис типа Mailgun / SendGrid / Postmark — выбор фиксируется на этапе деплоя). Креды — в Plesk Application Settings.
- **APNS** (iOS push): сертификат / Auth Key из Apple Developer Account. Загружается в Plesk Application Settings или в защищённый файл вне `\httpdocs`.
- **FCM** (Android push): Server Key / service account JSON из Firebase Console. Аналогично — в защищённое хранилище.

См. [notifications.md](./notifications.md) для бизнес-требований к уведомлениям.

## Регулярные задачи

Используется **Plesk Scheduled Tasks** (под капотом — Windows Task Scheduler):

- Очистка просроченных `InvitationToken` и `PasswordResetToken` — раз в час.
- Чистка/архивирование `AuditLog` старше N лет — раз в сутки.
- Триггер ретрая для `NotificationLog.Status = Failed` — каждые 5 минут.

Реализация — отдельный консольный артефакт (`Korendzh.Jobs.exe`), вызываемый по расписанию. Альтернатива — `BackgroundService` внутри веб-приложения, но при рестарте IIS он простаивает, поэтому критичные джобы лучше на Task Scheduler.

## Бэкапы

- **Plesk Backup Manager** настроен на **ежедневный** бэкап:
  - файлы (`\httpdocs`, конфиги, ключи push-провайдеров);
  - БД MSSQL.
- **Хранилище:** локально + удалённое (FTP/S3 — конфигурируется отдельно). Минимум одна копия вне сервера.
- **Retention:** 14 ежедневных + 4 еженедельных + 3 ежемесячных (рекомендация; уточняется по тарифу hoster.by).
- **Тест восстановления:** раз в квартал на staging-окружении.
- **Не покрывается Plesk-бэкапом:** значения переменных окружения, отдельно загруженные сертификаты — фиксируются в защищённом хранилище проектной документации (Vault / 1Password / KeePass — выбор команды).

## CI/CD: GitHub Actions

Workflow `.github/workflows/deploy.yml` уже включён в репозиторий и делает следующее:

1. Триггер — пуш тега `v*` либо ручной запуск (workflow_dispatch).
2. `dotnet restore` + `dotnet build` решения.
3. `dotnet publish src/Korendzh.Web/Korendzh.Web.csproj -c Release -o publish`.
4. Содержимое `publish/` коммитится в ветку `deploy` и `git push --force` пушит её в репозиторий.
5. Plesk видит пуш в `deploy` и обновляет `\httpdocs` автоматически.
6. Опциональный шаг: вызов `PLESK_WEBHOOK` (если задан в секретах) для принудительного pull со стороны Plesk.

Для отката — Run workflow вручную с указанием прежнего тега.

## Диагностика «сайт не открывается» / HTTP 500.30

ASP.NET Core на IIS падает на старте → IIS отдаёт 500.30. Алгоритм:

1. **Включить stdout-лог.** В `\httpdocs\web.config` поменять `stdoutLogEnabled="false"` на `"true"`. Создать в `\httpdocs` папку `logs\` (Plesk File Manager → Создать → Папка). Дать AppPool identity право записи на эту папку (Plesk → Permissions).
2. **Перезапустить приложение.** Plesk → IIS Settings → «Recycle» / `iisreset` (если есть RDP).
3. **Открыть сайт в браузере** — он отдаст 500.30. После этого в `\httpdocs\logs\stdout_*.log` появится файл с трассировкой исключения.
4. **Прочитать stack trace.** Типовые причины:
   - `InvalidOperationException: ConnectionStrings:Default is not configured` → создайте `appsettings.Local.json` (см. ниже).
   - `SqlException: Cannot open database 'Korendzh'` → база не создана в MSSQL, либо строка подключения неверная.
   - `Cannot find method` / `MissingMethodException` → не установлен .NET 8 Hosting Bundle. Запросите у hoster.by установку.
   - `Migration … not found` → не сгенерирована EF-миграция. Локально выполните `dotnet ef migrations add Initial --project src/Korendzh.Infrastructure --startup-project src/Korendzh.Web` и запушьте.
5. **После починки** — `stdoutLogEnabled` обратно в `false`, чтобы не флудить диск.

Дополнительно: Windows Event Viewer → `Application Log` сохраняет ASP.NET Core stack trace даже при выключенном stdout-логе.

## Чеклист первого деплоя

1. На сервере установлен Microsoft .NET Hosting Bundle нужной версии.
2. В Plesk создана БД MSSQL и пользователь с правами на неё.
3. В Plesk → Application Settings заданы все переменные окружения (DB, SMTP, Google OAuth, APNS/FCM, `Seed__AdminEmail`).
4. Репозиторий подключён, режим — auto, ветка — `deploy`.
5. Ветка `deploy` собрана через GitHub Actions из тега `v0.1.0`.
6. Plesk показал «Deployed successfully», в `\httpdocs` лежит `Korendzh.Web.dll` и `web.config`.
7. Запущен сайт — миграции EF Core отработали, в БД появились таблицы и сид-админ.
8. Сертификат Let's Encrypt выпущен на `бокатюк.бел`.
9. Принудительный редирект HTTP → HTTPS включён.
10. В Plesk Backup Manager настроено расписание и удалённое хранилище.
11. В Scheduled Tasks заведены джобы очистки токенов и ретрая нотификаций.
12. Сделан тестовый логин админом, тестовое создание менеджера, проверена доставка email-инвайта.

## Мобильное приложение (краткая заметка)

`.NET MAUI`-приложение собирается **отдельно** от веба и деплоится в магазины:

- **iOS** — через App Store Connect (требуется Apple Developer Program). Сборка через GitHub Actions runner с macOS либо локальная.
- **Android** — через Google Play Console (Internal testing → Closed → Production).
- API endpoints мобильного клиента указывают на `https://бокатюк.бел`.
- Мобильное приложение не публикуется на хостинг hoster.by — там только веб и API.

---

*Документ обновлён: 2026-04-29*
