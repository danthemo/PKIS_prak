# PostgreSQL package for TOIR administration

Проект содержит воспроизводимый локальный эксплуатационный набор PostgreSQL для системы технического обслуживания и ремонта. В составе есть схема БД, роли и права, тестовые данные, аудит, мониторинговые SQL-запросы, регламент обслуживания, резервное копирование и восстановление в отдельную проверочную базу.

API, UI и прикладный код в проект не входят.

## Структура

- `docker-compose.yml` - PostgreSQL 16, volume данных, healthcheck, монтирование SQL и backups.
- `.env.example` - пример локальных переменных окружения без реальных паролей.
- `sql/` - SQL-скрипты инициализации, прав, данных, аудита, мониторинга и обслуживания.
- `scripts/` - PowerShell и Bash-скрипты backup/restore.
- `backups/` - локальная папка для `.dump`-файлов.
- `docs/admin_checklist.md` - контрольный список администратора.

## Настройка окружения

Создайте локальный `.env` на основе примера:

```powershell
Copy-Item .env.example .env
```

Замените значения паролей в `.env` на локальные секреты. Файл `.env` не должен храниться в репозитории.

Роли `toir_app`, `toir_analyst`, `toir_auditor`, `toir_backup` создаются с плейсхолдерными паролями из SQL-скрипта. В реальном окружении замените их после запуска:

```sql
alter role toir_app password 'new_secret';
alter role toir_analyst password 'new_secret';
alter role toir_auditor password 'new_secret';
alter role toir_backup password 'new_secret';
```

## Запуск PostgreSQL

```powershell
docker compose up -d
```

Проверка контейнера:

```powershell
docker ps
docker logs toir-postgres
```

Проверка готовности:

```powershell
docker compose exec postgres pg_isready -U toir_admin -d toir_db
```

## Подключение

Если `psql` установлен локально:

```powershell
psql -h localhost -U toir_admin -d toir_db
```

Через контейнер:

```powershell
docker compose exec postgres psql -U toir_admin -d toir_db
```

## Применение SQL вручную

При первом запуске PostgreSQL автоматически выполняет файлы из `sql/`. Если база уже была создана ранее, init-скрипты повторно не запускаются. Для ручного применения:

```powershell
docker compose exec -T postgres psql -U toir_admin -d toir_db -f /docker-entrypoint-initdb.d/01_create_schemas.sql
docker compose exec -T postgres psql -U toir_admin -d toir_db -f /docker-entrypoint-initdb.d/02_create_tables.sql
docker compose exec -T postgres psql -U toir_admin -d toir_db -f /docker-entrypoint-initdb.d/03_create_roles.sql
docker compose exec -T postgres psql -U toir_admin -d toir_db -f /docker-entrypoint-initdb.d/04_grants.sql
docker compose exec -T postgres psql -U toir_admin -d toir_db -f /docker-entrypoint-initdb.d/05_seed_data.sql
docker compose exec -T postgres psql -U toir_admin -d toir_db -f /docker-entrypoint-initdb.d/06_indexes.sql
docker compose exec -T postgres psql -U toir_admin -d toir_db -f /docker-entrypoint-initdb.d/07_audit.sql
```

Файлы `08_monitoring_queries.sql` и `09_maintenance.sql` можно выполнять по необходимости.

## Проверка схемы и данных

```sql
select table_schema, table_name
from information_schema.tables
where table_schema in ('toir', 'audit')
order by table_schema, table_name;

select count(*) from toir.equipment;
select count(*) from toir.maintenance_requests;
select count(*) from toir.work_orders;
select count(*) from audit.audit_log;
```

## Резервное копирование

PowerShell:

```powershell
.\scripts\backup.ps1 -HostName localhost -Port 5432 -Database toir_db -User toir_admin -OutputDir backups
```

Bash:

```bash
./scripts/backup.sh localhost 5432 toir_db toir_admin backups
```

Если локальные `pg_dump`/`psql` не установлены, скрипты используют инструменты внутри контейнера `toir-postgres`.

## Восстановление

Восстановление выполняется в отдельную базу `toir_restore_check` и не изменяет основную `toir_db`.

PowerShell:

```powershell
.\scripts\restore.ps1 -DumpPath .\backups\toir_db_yyyyMMdd_HHmmss.dump
```

Автоматическая проверка последнего дампа:

```powershell
.\scripts\check_restore.ps1
```

Bash:

```bash
./scripts/restore.sh backups/toir_db_yyyyMMdd_HHmmss.dump
```

## Проверка прав ролей

Примеры проверок:

```sql
select grantee, table_schema, table_name, privilege_type
from information_schema.role_table_grants
where grantee in ('toir_app', 'toir_analyst', 'toir_auditor', 'toir_backup')
order by grantee, table_schema, table_name, privilege_type;

select has_table_privilege('toir_app', 'toir.maintenance_requests', 'delete') as app_can_delete_requests;
select has_table_privilege('toir_analyst', 'toir.maintenance_requests', 'select') as analyst_can_select_requests;
select has_table_privilege('toir_auditor', 'audit.audit_log', 'select') as auditor_can_select_audit;
```

## Мониторинг

Запустите набор запросов:

```powershell
docker compose exec -T postgres psql -U toir_admin -d toir_db -f /docker-entrypoint-initdb.d/08_monitoring_queries.sql
```

В файле есть запросы по активным соединениям, долгим запросам, блокировкам, размерам БД и таблиц, событиям аудита и статусам заявок.

## Обслуживание

Выполнение регламентных команд:

```powershell
docker compose exec -T postgres psql -U toir_admin -d toir_db -f /docker-entrypoint-initdb.d/09_maintenance.sql
```

Файл содержит примеры `analyze`, `vacuum analyze`, `reindex`, проверку dead tuples и оценку размера индексов.
