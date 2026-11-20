# Результат целевой проверки состава кромки ЛК ВЕК

Дата: 20 ноября 2026 года. Проверена обновлённая локальная сборка Catalog API на `https://catalog-api.debug.feach.ru:5005`; данные и ключи Data Protection — `mm-debug`.

Из `CuttingMaterialEdgeDto` в ответе `GET /api/for-vek-lk/materials/{id}` удалены `manufacturer` и `stockQuantity`, включая их чтение из БД и передачу в результат. `isStockItem` и `markings` сохранены.

- `dotnet build catalog-api/catalog-api.slnx --no-restore` — успешно, предупреждений и ошибок нет.
- `dotnet format catalog-api/catalog-api.slnx --verify-no-changes --no-restore` — успешно.
- После перезапуска API проверен `/swagger/v1/swagger.json`: схема `CuttingMaterialEdgeDto` содержит ровно 11 ожидаемых обязательных полей с описаниями; `manufacturer` и `stockQuantity` отсутствуют.
- `git diff --check` — успешно.
- Полный регресс и повторный функциональный прогон не запускались; тестовые записи не создавались.
- API перезапущен с прежними настройками на порту 5005 и оставлен включённым. Миграции не требуются.