# Мобильное приложение

`.NET MAUI`-проект, единый кодбейс для **iOS** и **Android**. Адресован, в первую очередь, воркерам — быстрый ввод часов с поля.

Связано с [system-overview.md](./system-overview.md), [user-stories.md](./user-stories.md), [requirements.md](./requirements.md).

## Расположение

`src/Korendzh.Mobile/` — отдельный проект, **не** включён в `Korendzh.sln` по умолчанию (требует MAUI workload, который не везде установлен).

Чтобы добавить вручную:

```bash
dotnet sln Korendzh.sln add src/Korendzh.Mobile/Korendzh.Mobile.csproj
```

## Структура

```
Korendzh.Mobile/
  App.xaml + App.xaml.cs
  AppShell.xaml + AppShell.xaml.cs
  MauiProgram.cs                  # DI, регистрация страниц
  Pages/
    LoginPage.xaml + .xaml.cs     # Email + пароль → JWT
    EntriesPage.xaml + .xaml.cs   # Список своих TimeEntry
    CreateEntryPage.xaml + .xaml.cs # Создание новой записи
  Services/
    AuthState.cs                  # Хранение JWT в памяти + SecureStorage
    KorendzhApiClient.cs          # HttpClient к /api/auth и /api/timeentries
  Platforms/
    Android/                      # MainActivity, MainApplication, AndroidManifest.xml
    iOS/                          # AppDelegate, Program.cs, Info.plist
```

## Аутентификация

- API: `POST /api/auth/login` отдаёт JWT (24 часа по умолчанию).
- Токен и срок действия сохраняются в `SecureStorage` (Keychain на iOS, KeyStore на Android).
- При старте `LoginPage.OnAppearing` пытается загрузить сохранённый токен; если валиден — сразу переходит на `EntriesPage`.
- Logout очищает SecureStorage и сбрасывает `AuthState`.

## API-эндпоинты, используемые мобильным

| Метод | Путь | Назначение |
|---|---|---|
| `POST` | `/api/auth/login` | Email+пароль → JWT |
| `GET` | `/api/auth/me` | Текущий пользователь и роли |
| `GET` | `/api/timeentries?from=…&to=…` | Список записей текущего воркера |
| `POST` | `/api/timeentries` | Создать запись |

Все защищённые ручки требуют заголовок `Authorization: Bearer <jwt>`.

## Сборка

Требуется .NET MAUI workload:

```bash
dotnet workload install maui
```

Затем:

```bash
# Android (требуется Android SDK)
dotnet build src/Korendzh.Mobile -f net8.0-android

# iOS (только на macOS, требует Xcode)
dotnet build src/Korendzh.Mobile -f net8.0-ios
```

## Что предстоит доделать

- App icon / Splash screen — сейчас используются дефолты MAUI; заменить перед публикацией.
- Регистрация push-токена при логине (`POST /api/push/register` — endpoint не реализован).
- Обработка офлайн-сценариев — пока приложение «онлайн-only» (см. [system-overview.md](./system-overview.md)).
- Локальная валидация — продублировать [validation.md](./validation.md).
- Автокомплит автомобилей в `CreateEntryPage` (использовать `/api/cars/autocomplete`).
- Pull-to-refresh на `EntriesPage`.
- Экраны редактирования и удаления записи.
- Биометрия (Face ID / отпечаток) для повторного входа без ввода пароля.

## Дистрибуция

Мобильное приложение **не** деплоится через Plesk. Каналы:

- **iOS** — App Store Connect (требует Apple Developer Program $99/год).
- **Android** — Google Play Console (Internal → Closed → Production), либо APK для стороннего распространения.

Сборка релизных артефактов — через GitHub Actions с macOS-runner-ом для iOS и Ubuntu для Android. Workflow добавляется отдельно от веб-деплоя.

---

*Документ обновлён: 2026-04-29*
