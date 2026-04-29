# Модель данных

Описание ключевых сущностей системы. Конкретные типы и ограничения СУБД (Microsoft SQL Server 2019) уточняются на этапе реализации.

Связано с [validation.md](./validation.md), [user-stories.md](./user-stories.md), [notifications.md](./notifications.md).

## Сущности

### User (Пользователь)

Единая таблица пользователей с разделением по роли.

| Поле | Тип | Описание |
|---|---|---|
| Id | GUID / int | Идентификатор |
| Email | string | Уникальный email (логин) |
| PasswordHash | string | Хэш пароля (если используется парольная аутентификация) |
| GoogleSubject | string? | Идентификатор Google-аккаунта (для OAuth-входа) |
| FullName | string | ФИО |
| Role | enum | `Admin` / `Manager` / `Worker` |
| DivisionId | FK → Division | Подразделение, к которому относится пользователь (для Worker — обязательно; для Manager — собственное; для Admin — null) |
| IsActive | bool | Активен ли аккаунт |
| EmailNotificationsEnabled | bool | Подписка на нетранзакционные email-уведомления (изменения записей). Дефолт `true`. |
| TimeZone | string? | IANA-зона пользователя (напр., `Europe/Minsk`) для корректной интерпретации `WorkDate`. Если null — берётся зона по умолчанию из настроек. |
| CreatedAt | datetime | Дата создания |

Один воркер принадлежит одному подразделению. Один менеджер — владелец одного подразделения.

### Division (Подразделение)

| Поле | Тип | Описание |
|---|---|---|
| Id | GUID / int | Идентификатор |
| Name | string | Название подразделения, уникальное |
| ManagerId | FK → User | Менеджер, ответственный за подразделение |
| IsArchived | bool | Архивированное подразделение не принимает новые `TimeEntry`, история сохраняется |
| CreatedAt | datetime | Дата создания |

### TimeEntry (Запись о рабочих часах)

Основная транзакционная сущность.

| Поле | Тип | Описание |
|---|---|---|
| Id | GUID / int | Идентификатор |
| WorkerId | FK → User | Воркер, к которому относится запись |
| WorkDate | date | Дата выполнения работы |
| Hours | decimal(5,2) | Количество отработанных часов |
| TaskName | string | Название задачи |
| CarId | FK → Car? | Автомобиль из справочника (опционально) |
| LicensePlate | string | Государственный номер (фиксируется на момент записи) |
| Description | string | Краткое описание |
| CreatedBy | FK → User | Кто создал запись (воркер или менеджер/админ от его имени) |
| CreatedAt | datetime | Когда создано |
| UpdatedBy | FK → User? | Кто последний редактировал |
| UpdatedAt | datetime? | Когда последний раз редактировалось |
| RowVersion | rowversion / int | Для optimistic concurrency (см. [validation.md](./validation.md)) |
| IsDeleted | bool | Soft-delete флаг |
| DeletedBy | FK → User? | Кто удалил |
| DeletedAt | datetime? | Когда удалено |

`LicensePlate` хранится как отдельное поле (а не только через FK на `Car`), чтобы исторические записи не менялись при правке справочника.

### Car (Автомобиль / справочник)

Общий справочник автомобилей. Записи добавляются как воркерами (через автокомплит формы), так и менеджерами/админами.

| Поле | Тип | Описание |
|---|---|---|
| Id | GUID / int | Идентификатор |
| Name | string | Название / марка / модель |
| LicensePlate | string | Государственный номер (если жёстко закреплён за автомобилем) |
| IsActive | bool | Скрыт ли из автокомплита |
| CreatedBy | FK → User | Кто добавил |
| CreatedAt | datetime | Когда добавлено |

Поведение автокомплита: при вводе ищется по `Name` и `LicensePlate` среди `IsActive = true`. Если ничего не подошло — пользователь может ввести новую строку, при сохранении `TimeEntry` будет создана новая запись `Car`.

### InvitationToken (Приглашение)

| Поле | Тип | Описание |
|---|---|---|
| Id | GUID | Идентификатор |
| UserId | FK → User | Кому выдан |
| TokenHash | string | Хэш токена (исходное значение в БД не хранится) |
| CreatedBy | FK → User | Кто пригласил |
| CreatedAt | datetime | Когда создан |
| ExpiresAt | datetime | Когда истекает (по умолчанию +7 дней) |
| ConsumedAt | datetime? | Когда использован |

### PasswordResetToken (Сброс пароля)

| Поле | Тип | Описание |
|---|---|---|
| Id | GUID | Идентификатор |
| UserId | FK → User | Кому выдан |
| TokenHash | string | Хэш токена |
| CreatedAt | datetime | Когда создан |
| ExpiresAt | datetime | Когда истекает (по умолчанию +1 час) |
| ConsumedAt | datetime? | Когда использован |

### PushDevice (Мобильное устройство)

| Поле | Тип | Описание |
|---|---|---|
| Id | GUID | Идентификатор |
| UserId | FK → User | Владелец |
| Platform | enum | `iOS` / `Android` |
| PushToken | string | APNS / FCM токен |
| LastSeenAt | datetime | Последний логин с этого устройства |
| IsActive | bool | Если провайдер ответил `Unregistered` — выставляется `false` |

### AuditLog

Запись каждого создания / изменения / удаления значимых сущностей (минимум — `TimeEntry`, опционально — `User`, `Division`, `Car`).

| Поле | Тип | Описание |
|---|---|---|
| Id | GUID / bigint | Идентификатор |
| EntityType | string | `TimeEntry` / `User` / `Division` / `Car` |
| EntityId | string | Id затронутой записи |
| Action | enum | `Created` / `Updated` / `Deleted` |
| ActorId | FK → User | Кто выполнил действие |
| At | datetime | Когда (UTC) |
| BeforeJson | string? | Снимок «до» (для Updated/Deleted) |
| AfterJson | string? | Снимок «после» (для Created/Updated) |

### NotificationLog

Лог отправленных уведомлений (см. [notifications.md](./notifications.md)).

| Поле | Тип | Описание |
|---|---|---|
| Id | GUID / bigint | Идентификатор |
| UserId | FK → User | Получатель |
| Channel | enum | `Email` / `Push` |
| TemplateTag | string | Тег шаблона, напр. `timeentry.edited_by_manager` |
| EventKey | string | Уникальный ключ события (для идемпотентности) |
| PayloadJson | string | Контекст шаблона |
| Status | enum | `Queued` / `Sent` / `Failed` |
| AttemptCount | int | Сколько раз пробовали |
| CreatedAt | datetime | Когда поставлено в очередь |
| SentAt | datetime? | Когда успешно отправлено |
| FailureReason | string? | Если `Failed` |

## Связи

```
Division 1 ── 1 Manager (User, role=Manager)
Division 1 ── * Worker  (User, role=Worker)

Worker (User) 1 ── * TimeEntry
Car           1 ── * TimeEntry  (опционально)
User          1 ── * Car        (как создатель)
```

Admin (User, role=Admin) не привязан к Division — видит все.

## Заметки по реализации

- Пароли — только хэш (bcrypt / Argon2), исходный пароль никогда не хранится.
- Все datetime в БД — в UTC; конвертация в локальное время на клиенте.
- Soft delete для `User` (через `IsActive`), чтобы не терять историю `TimeEntry`.
- Soft delete для `TimeEntry` (через `IsDeleted`) — нужен и для возможности восстановления, и для стабильности audit log.
- Удаление `Car` из справочника не должно ломать исторические `TimeEntry` (`Car.IsActive = false`, FK сохраняется).
- Токены (Invitation, PasswordReset) хранятся **только как хэш** — оригинальный токен виден один раз в email и больше нигде.
- AuditLog растёт быстро; настроить ретеншен (например, 2 года) и индексы по `EntityType + EntityId + At`.

## Сидинг при деплое

При первом развёртывании запускается миграция, которая создаёт:

- Один `Admin`-аккаунт с email из переменной окружения и сгенерированным временным паролем (или через ручной запуск с параметром).
- (Опционально) демо-`Division` для smoke-теста, удаляемое в проде.

Дальнейшее наполнение — через UI и email-инвайты (см. [user-stories.md](./user-stories.md), US-A6).

---

*Документ обновлён: 2026-04-29*
