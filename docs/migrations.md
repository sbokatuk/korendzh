# Работа с базой данных и миграциями

Этот документ описывает **повседневный воркфлоу**: как менять схему БД и накатывать изменения на прод без пересоздания базы.

Связано с [data-model.md](./data-model.md), [deployment.md](./deployment.md).

## TL;DR

- **Любое изменение модели** → добавить миграцию: `dotnet ef migrations add <Name>` локально, закоммитить.
- **Деплой** автоматически применяет pending миграции через `db.Database.MigrateAsync()` на старте приложения.
- **Пересоздавать БД больше не нужно** — миграции применяются накатом.

## Текущее состояние

В `Program.cs` есть умный фолбэк:

```csharp
var hasMigrations = db.Database.GetMigrations().Any();
if (hasMigrations)
    await db.Database.MigrateAsync();   // обычный путь
else
    await db.Database.EnsureCreatedAsync(); // bootstrap, только если миграций ещё нет в коде
```

Если в `src/Korendzh.Infrastructure/Migrations/` **нет ни одной миграции**, EF создаёт схему через `EnsureCreated()` без таблицы `__EFMigrationsHistory`. Это бутстрап-режим — годен только для первого старта на чистой БД.

Как только в коде появляется хотя бы одна миграция, `MigrateAsync` берёт верх и применяет всё, что не накатано.

## Переход на «настоящие» миграции (одноразово)

**На этом шаге БД нужно пересоздать ровно один раз.** Дальше она будет жить и пополняться миграциями инкрементально.

### Шаг 1. Сгенерировать первую миграцию локально

Установить EF tools, если ещё нет:

```bash
dotnet tool install --global dotnet-ef --version "8.*"
```

> На zsh (macOS) кавычки вокруг `"8.*"` обязательны, иначе шелл попытается раскрыть глоб и команда упадёт с `zsh: no matches found: 8.*`.

После установки убедитесь, что `~/.dotnet/tools` в `$PATH`:

```bash
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc
source ~/.zshrc
dotnet ef --version
```

Из корня репозитория:

```bash
dotnet ef migrations add Initial --project src/Korendzh.Infrastructure --startup-project src/Korendzh.Web
```

В `src/Korendzh.Infrastructure/Migrations/` появится три файла:
- `<timestamp>_Initial.cs` — сама миграция.
- `<timestamp>_Initial.Designer.cs` — снэпшот модели на момент миграции.
- `AppDbContextModelSnapshot.cs` — текущий снэпшот, обновляется с каждой новой миграцией.

> Если команда требует connection string и не находит её — добавьте в user-secrets рабочую строку (можно фиктивную, лишь бы провайдер MSSQL мог её распарсить):
> ```bash
> cd src/Korendzh.Web
> dotnet user-secrets set "ConnectionStrings:Default" "Server=(localdb)\\MSSQLLocalDB;Database=Korendzh;Trusted_Connection=True"
> ```
> На Mac/Linux нет LocalDB, поднимите MSSQL в Docker:
> ```bash
> docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Pass1" \
>   -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
> ```

### Шаг 2. Закоммитить миграцию

```bash
git add src/Korendzh.Infrastructure/Migrations
git commit -m "Initial EF migration (snapshot of current schema)"
git push origin main
git tag v0.2.0 && git push origin v0.2.0  # триггерит деплой workflow
```

### Шаг 3. Пересоздать прод БД

В Plesk → Базы данных → удалить `bokatyukbe_korendzh` → создать заново с тем же именем. Connection string в `\httpdocs\appsettings.Local.json` остаётся прежний.

### Шаг 4. Положить app_offline.htm и обновить код

В `\httpdocs` через File Manager создать пустой `app_offline.htm` (любой HTML), Plesk → Git → Pull Updates, удалить `app_offline.htm`.

При старте приложение:
1. Видит миграцию в коде.
2. Видит чистую БД без `__EFMigrationsHistory`.
3. `MigrateAsync()` создаёт `__EFMigrationsHistory`, применяет `Initial`, регистрирует её.
4. `DataSeeder` создаёт роли, админа, дефолтные SiteSettings/Pages/Services.
5. Сайт запускается.

С этого момента БД отслеживается миграциями.

## Альтернатива без пересоздания (если данных уже жалко)

Если в БД есть данные, которые нельзя терять, можно «сблагословить» текущую схему как уже-применённую `Initial`:

### Шаг 1. Сгенерировать миграцию (как в основном пути)

```bash
dotnet ef migrations add Initial --project src/Korendzh.Infrastructure --startup-project src/Korendzh.Web
```

### Шаг 2. Найти её ID

В `src/Korendzh.Infrastructure/Migrations/` появится файл `20260429120000_Initial.cs` — ID это `20260429120000_Initial` (timestamp + имя).

### Шаг 3. Вручную создать `__EFMigrationsHistory` и пометить Initial как применённую

В SSMS или через Plesk → Базы данных → Управление базами данных, выполнить:

```sql
CREATE TABLE [dbo].[__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);

INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES ('20260429120000_Initial', '8.0.10');  -- замените ID на реальный из файла
```

### Шаг 4. Деплой

После следующего деплоя `MigrateAsync` увидит, что Initial уже применена, и **не выполнит `CREATE TABLE`** повторно. Все будущие миграции будут накатываться нормально.

⚠️ Этот путь требует, чтобы текущая схема БД **точно совпадала** с тем, что эмитит Initial-миграция. Если EnsureCreated создал её из старой версии модели, а вы успели поменять модель до генерации Initial — будет рассинхрон. В этом случае проще выбрать основной путь (пересоздать БД).

## Повседневный воркфлоу (после перехода)

Каждое изменение модели — отдельная миграция:

### Пример: добавить поле `Service.WarrantyMonths`

1. **Меняем сущность** в `src/Korendzh.Domain/Cms/Service.cs`:
   ```csharp
   public int WarrantyMonths { get; set; } = 6;
   ```

2. **Добавляем миграцию** локально:
   ```bash
   dotnet ef migrations add AddServiceWarrantyMonths \
     --project src/Korendzh.Infrastructure --startup-project src/Korendzh.Web
   ```

   EF сгенерит `<timestamp>_AddServiceWarrantyMonths.cs` с примерно таким `Up()`:
   ```csharp
   migrationBuilder.AddColumn<int>(
       name: "WarrantyMonths",
       table: "Services",
       type: "int",
       nullable: false,
       defaultValue: 6);
   ```

3. **Прогоняем локально**, чтобы убедиться:
   ```bash
   dotnet ef database update --project src/Korendzh.Infrastructure --startup-project src/Korendzh.Web
   ```
   Локальная dev-БД обновится. Запускаем приложение, проверяем.

4. **Коммитим и пушим**:
   ```bash
   git add src/Korendzh.Infrastructure/Migrations src/Korendzh.Domain/Cms/Service.cs
   git commit -m "Service: add WarrantyMonths"
   git push
   ```

5. **Деплой через тег или ручной запуск workflow** → Plesk → положить `app_offline.htm` → Pull → удалить `app_offline.htm`.

6. На старте приложение увидит pending миграцию `AddServiceWarrantyMonths`, применит, добавит колонку. Существующие данные не теряются.

## Полезные команды

| Команда | Что делает |
|---|---|
| `dotnet ef migrations add <Name>` | Создать новую миграцию по diff между моделью и последним снэпшотом |
| `dotnet ef migrations remove` | Откатить последнюю **несконсолидированную** миграцию (только если не применена в БД) |
| `dotnet ef migrations list` | Список всех миграций (применённые/нет) |
| `dotnet ef database update` | Применить все pending миграции к локальной БД |
| `dotnet ef database update <Name>` | Откатить/перейти на конкретную миграцию (при движении назад потребуется `Down()`) |
| `dotnet ef migrations script` | Сгенерировать SQL для всех миграций — полезно если хочется руками выполнить на проде |
| `dotnet ef migrations script <From> <To>` | SQL только для диапазона миграций |
| `dotnet ef migrations bundle -o ef.exe` | Самостоятельный бинарь, применяет миграции — альтернатива `MigrateAsync` для CI/CD |
| `dotnet ef dbcontext info` | Проверить, что connection string и DbContext успешно резолвятся |

Все команды выполняются из корня репо с `--project src/Korendzh.Infrastructure --startup-project src/Korendzh.Web` (или из `src/Korendzh.Web` без флагов).

## Если миграция «не та» — как откатить

### Локально, до коммита

```bash
dotnet ef migrations remove --project src/Korendzh.Infrastructure --startup-project src/Korendzh.Web
```

Удалит файлы последней миграции, обновит снэпшот. Только если она не применена к БД (или применена и потом откатили `dotnet ef database update <PreviousName>`).

### Локально, после применения

1. Откатиться на предыдущую: `dotnet ef database update PreviousMigrationName`
2. Удалить миграцию: `dotnet ef migrations remove`

### На проде, после деплоя

Серьёзно: лучше написать **новую** миграцию, которая откатывает предыдущую (например, удаляет неудачно добавленную колонку). Это нагляднее и трекается в истории. EF её сам не предложит — пишите руками.

```bash
dotnet ef migrations add RevertWarrantyMonths --project src/Korendzh.Infrastructure --startup-project src/Korendzh.Web
# отредактировать сгенерённый файл, оставив только нужные операции
```

## Опасные сценарии

**Не делайте** `dotnet ef migrations remove` после того, как миграция уже **применена на проде** и закоммичена. Локальный снэпшот разойдётся с продом, следующая миграция будет неконсистентной. Если очень нужно — пишите явную обратную миграцию.

**Не правьте старые миграции после применения.** Только новая миграция поверх. Иначе на старых деплоях не сходится.

**Большие миграции (на огромной таблице)** — выводите сайт в `app_offline.htm` на время выполнения. Если миграция > нескольких секунд — лучше отдельный SQL-скрипт в окно обслуживания, а в коде эту миграцию пометить «уже применённой» через `__EFMigrationsHistory`.

## Откатить EnsureCreated-фолбэк (опционально)

После перехода на нормальные миграции `EnsureCreated`-ветка в Program.cs больше не нужна — на проде всегда есть Initial миграция. Можно оставить для упрощённого первого запуска новых сред (тест-стенд, локалка).

Если хотите убрать совсем — замените блок в `Program.cs` на просто:

```csharp
await db.Database.MigrateAsync();
```

Тогда без миграций приложение упадёт — это явное требование, чтобы кто-то не забыл сгенерировать `Initial`.

---

*Документ создан: 2026-04-29*
