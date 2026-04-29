# Документация проекта `korendzh`

Система учёта рабочего времени с веб- и мобильным приложениями. Воркеры отправляют отчёты, менеджеры ведут учёт по подразделению, админы управляют всей организацией.

Домен: [бокатюк.бел](http://бокатюк.бел)

## Содержание

- [system-overview.md](./system-overview.md) — что делает система: роли, сценарии, lifecycle, безопасность.
- [user-stories.md](./user-stories.md) — полный перечень юзеркейсов (US-W*, US-M*, US-A*, US-S*).
- [roles-permissions.md](./roles-permissions.md) — матрица прав Worker / Manager / Admin.
- [data-model.md](./data-model.md) — сущности БД (User, Division, TimeEntry, Car, токены, audit log) и их связи.
- [validation.md](./validation.md) — правила валидации полей и ограничения данных.
- [notifications.md](./notifications.md) — матрица email и push уведомлений.
- [requirements.md](./requirements.md) — инфраструктура и технологический стек (Windows Server 2025, IIS, .NET, MS SQL Server, .NET MAUI и т.д.).
- [deployment.md](./deployment.md) — разворачивание на hoster.by + Plesk: ветка `deploy`, миграции, SSL, бэкапы, scheduled tasks.

## Соглашения

- Документация ведётся в этой папке `/docs`.
- При любых изменениях системы документация обновляется в том же коммите.
- Все даты в документах — в формате `YYYY-MM-DD`.
- В каждом документе внизу — поле `Документ обновлён: …`.

---

*Индекс обновлён: 2026-04-29*
