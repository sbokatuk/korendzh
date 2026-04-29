# Технические требования

Этот документ описывает **инфраструктуру и технологический стек**. Что именно делает система — см. [system-overview.md](./system-overview.md). Модель данных — [data-model.md](./data-model.md). Роли и права — [roles-permissions.md](./roles-permissions.md). Как разворачивается — [deployment.md](./deployment.md).

## Хостинг

- **Провайдер:** hoster.by
- **Хост:** `w14.hoster.by`
- **Панель управления:** Plesk (Windows-edition)
- **Авто-деплой:** Plesk Git extension, ветка `deploy` репозитория `sbokatuk/korendzh` в `\httpdocs`

Подробности — в [deployment.md](./deployment.md).

## Доменное имя

- **Домен:** [бокатюк.бел](http://бокатюк.бел)
- **Punycode:** `xn--80aaaifc7a8azal.xn--90ais`
- **SSL:** Let's Encrypt через Plesk

## Серверная инфраструктура

### Операционная система
- Windows Server 2025

### Веб-сервер
- Internet Information Services (IIS) 10.0

### Дополнительные модули IIS
- IIS URL Rewrite 2.1

## Технологический стек

### .NET Framework
Поддерживаемые версии:
- 2.0
- 3.5
- 4.0
- 4.5.x
- 4.6.x
- 4.7.x
- 4.8.x

### ASP.NET MVC
Поддерживаемые версии: 1, 2, 3, 4, 5, 6

### .NET Core / .NET
Поддерживаемые версии:
- .NET Core 1.x
- .NET Core 2.x
- .NET Core 3.x
- .NET 5.x
- .NET 6.x
- .NET 7.x
- .NET 8.x
- .NET 9.x
- .NET 10.x

### Прочие технологии
- Node.js
- Silverlight 4, 5
- WCF (Windows Communication Foundation)
- AJAX

## База данных

- **СУБД:** Microsoft SQL Server 2019

## Файловые сервисы

- **FTP-сервер:** Windows FTP-server с изоляцией пользователей (User Isolation)

## Мобильное приложение

- **Платформа:** .NET MAUI
- **Целевые ОС:** iOS и Android (общий кодбейс)
- **Аутентификация:** общая с веб-приложением (email+пароль и Google OAuth)

## Аутентификация и интеграции

- Email + пароль (с восстановлением через email)
- Google OAuth 2.0 (Sign in with Google)

---

*Документ обновлён: 2026-04-29*
