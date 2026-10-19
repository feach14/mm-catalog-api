# Последняя адресная проверка

Дата: 2026-10-19. Проект: `mm-catalog-api`. Область: размеры листа в `GET /api/for-calculate/materials/{id}`.

В `GetMaterialForCalculateQueryResult` добавлены обязательные целочисленные `width` и `height` в миллиметрах. Projection читает их из связанного `MaterialSheetSize`; прежние `id`, `kvM` и `perimetrM` сохранены. Ожидания существующих CALC-11—16 и HTTP-раннера актуализированы.

## Результаты

- `dotnet build catalog-api/catalog-api.slnx --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false` — успешно, 0 ошибок и предупреждений.
- `dotnet format catalog-api/catalog-api.slnx --verify-no-changes --no-restore` — успешно.
- Синтаксис существующего Python-раннера, `git diff --check`, UTF-8 без BOM, LF и отсутствие завершающего LF в изменённых файлах — успешно.
- Реальный `GET /swagger/v1/swagger.json` — HTTP 200. Схема `GetMaterialForCalculateQueryResult` содержит ровно пять обязательных полей: `id`, `kvM`, `perimetrM`, `width`, `height`; `width` и `height` имеют тип `integer` и описание единиц в миллиметрах.
- Анонимный `GET /api/for-calculate/materials/133` — HTTP 200: `id=133`, `kvM=4.1`, `perimetrM=9.16`, `width=1830`, `height=2750`.

Проверены HTTP-контракт и чтение существующего материала. Полный регресс, полные сценарии CALC-11—16 с ролевыми проверками и изменением тестовых материалов не запускались. Внутренние проверки через DI, модели тестов, EF и прямые запросы к БД не выполнялись.

Перед запуском порт `5005` был свободен. Использован собственный DEV-процесс на `https://catalog-api.debug.feach.ru:5005` со штатным профилем `Debug`, временным `--urls` и `Database=mm-debug` в переменных окружения двух подключений. Постоянные адреса, порты и настройки не менялись. После завершения проверок собственный процесс остановлен; `lsof` подтвердил отсутствие слушателя на порту `5005`.

Тестовые данные и временные файлы не создавались. Core, настройки, миграции и staging сохранены.