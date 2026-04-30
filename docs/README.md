# Документация проекта `korendzh`

**Korendzh** — это:

1. Публичный лендинг СТО (услуги, отзывы, контакты), наполняемый из админки.
2. Внутренняя система учёта рабочего времени для сотрудников СТО — после входа.

Один сайт, один деплой, один домен. Анонимный посетитель видит лендинг и кнопку «Войти» справа вверху; авторизованный сотрудник попадает в свой кабинет (учёт часов).

Домен: [бокатюк.бел](http://бокатюк.бел)

## Содержание

- [system-overview.md](./system-overview.md) — что делает система: роли, сценарии, lifecycle, безопасность.
- [user-stories.md](./user-stories.md) — полный перечень юзеркейсов (US-W*, US-M*, US-A*, US-S*).
- [roles-permissions.md](./roles-permissions.md) — матрица прав Worker / Manager / Admin.
- [data-model.md](./data-model.md) — сущности БД (User, Division, TimeEntry, Car, токены, audit log) и их связи.
- [migrations.md](./migrations.md) — повседневный воркфлоу EF Core миграций (как менять схему без пересоздания БД).
- [app-mode.md](./app-mode.md) — режимы работы (`Full` / `TrackingOnly`) и как переключать.
- [plan.md](./plan.md) — план загрузки: календарь часов, шаблоны графиков, статистика «План vs Факт».
- [validation.md](./validation.md) — правила валидации полей и ограничения данных.
- [notifications.md](./notifications.md) — матрица email и push уведомлений.
- [api.md](./api.md) — REST API (JWT, `/api/auth/login`, `/api/timeentries`).
- [mobile.md](./mobile.md) — мобильное приложение (.NET MAUI): структура и что доделать.
- [cms.md](./cms.md) — публичный лендинг СТО + CMS (услуги, отзывы, страницы).
- [requirements.md](./requirements.md) — инфраструктура и технологический стек (Windows Server 2025, IIS, .NET, MS SQL Server, .NET MAUI и т.д.).
- [deployment.md](./deployment.md) — разворачивание на hoster.by + Plesk: ветка `deploy`, миграции, SSL, бэкапы, scheduled tasks.

## Соглашения

- Документация ведётся в этой папке `/docs`.
- При любых изменениях системы документация обновляется в том же коммите.
- Все даты в документах — в формате `YYYY-MM-DD`.
- В каждом документе внизу — поле `Документ обновлён: …`.

---

*Индекс обновлён: 2026-04-29*
