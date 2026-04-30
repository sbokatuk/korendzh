# Режимы работы приложения

Приложение АрВи-транс (кодовое имя проекта — `korendzh`) поддерживает два режима, переключаемых через конфигурацию (`web.config`-окружение или `appsettings.Local.json`):

| Режим | Что включено | Когда использовать |
|---|---|---|
| **`Full`** *(по умолчанию)* | Публичный лендинг СТО + CMS + личный кабинет (учёт времени, статистика, план, управление пользователями) | Когда нужен сайт-визитка вместе с внутренней системой |
| **`TrackingOnly`** | Только личный кабинет: учёт времени, статистика «План vs Факт», план, пользователи (мастера / работники / подразделения), тест email, аудит | Когда сайт развёртывается чисто как корпоративный таймтрекер без публичного лендинга |

## Как переключить

### Вариант A. Через `web.config` (на сервере)

В Plesk → File Manager отредактируйте `\httpdocs\web.config`, в блоке `<aspNetCore>` добавьте переменную окружения:

```xml
<aspNetCore processPath="dotnet"
            arguments=".\Korendzh.Web.dll"
            stdoutLogEnabled="false"
            stdoutLogFile=".\logs\stdout"
            hostingModel="InProcess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="App__Mode" value="TrackingOnly" />
  </environmentVariables>
</aspNetCore>
```

Сохранение `web.config` автоматически recycl-ит App Pool.

> **Замечание:** при следующем деплое из ветки `deploy` файл `web.config` перезапишется значениями из репозитория. Чтобы режим переживал деплои, используйте вариант B.

### Вариант B. Через `\httpdocs\appsettings.Local.json`

Этот файл не в репозитории и не перезаписывается деплоем. Добавьте в него:

```json
{
  "App": { "Mode": "TrackingOnly" }
}
```

Сохранение этого файла триггерит recycle (ASP.NET Core отслеживает изменения с `reloadOnChange: true`).

После рестарта в stdout-логе появится строка:

```
info: Startup[0]
      App mode: TrackingOnly
```

## Что меняется в `TrackingOnly`

### Маршрутизация

| Путь | Поведение |
|---|---|
| `/` | Аноним → `/Account/Login`. Работник → `/TimeEntries/Create`. Мастер/админ → `/Dashboard`. |
| `/services`, `/services/{slug}` | **404** |
| `/reviews` | **404** |
| `/contacts` | **404** |
| `/p/{slug}` | **404** |
| `/Admin/Cms/*` | **404** |
| `/TimeEntries`, `/Plan`, `/Statistics`, `/Workers`, `/Cars`, `/Admin/Managers`, `/Admin/Divisions`, `/Admin/EmailTest` | Работают как обычно |

### Навигация

В шапке и на дашборде скрываются пункты «Сайт», «Услуги», «Отзывы», «Страницы», «→ Сайт». Остаётся только трекер + CMS пользователей.

### CMS-данные в БД

Таблицы `SiteSettings`, `Services`, `Reviews`, `Pages`, `MediaAssets` остаются — данные не удаляются. Если потом переключить обратно в `Full`, всё на месте. Сидер дефолтных страниц/услуг продолжает работать (наличие данных в БД ничему не мешает).

## Реализация

- [`AppOptions`](../src/Korendzh.Web/Configuration/AppOptions.cs) — enum + класс настроек, биндится из `App` секции.
- В `Program.cs` после `UseRouting` стоит middleware, который для `TrackingOnly` возвращает 404 на публичные/CMS пути.
- `IndexModel.OnGetAsync` сам выбирает поведение в зависимости от режима.
- `_Layout.cshtml` и `Dashboard/Index.cshtml` инжектят `IOptions<AppOptions>` и условно скрывают пункты меню.
- На старте в stdout-лог пишется `App mode: …` для проверки.

## Тестовые сценарии

После переключения в `TrackingOnly` стоит проверить:

1. `https://бокатюк.бел/` без логина → редирект на `/Account/Login`.
2. `https://бокатюк.бел/services` → 404.
3. Логин админом → попадает на `/Dashboard`, в меню нет CMS-пунктов.
4. Логин работником → попадает на `/TimeEntries/Create`.
5. `https://бокатюк.бел/Admin/Cms/SiteSettings` (прямой ввод) → 404.
6. Существующая БД с CMS-данными не повреждается.

После переключения обратно в `Full`:
1. `/` показывает лендинг.
2. CMS-пункты в меню снова видны.
3. Услуги/отзывы из БД отображаются как и были.

---

*Документ создан: 2026-04-29*
