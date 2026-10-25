# mm-catalog-api

## Каталог кромок

CRUD кромок и справочников производителей, толщин и единиц продажи расположен под `/api/for-admin/edges`. Список кромок вместе с фильтрами возвращается одним запросом `GET /api/for-admin/edges/compact`. Изображения загружаются через административный API кромок и читаются через `/api/edges/images/{fileGuid}`. Общий порядок материалов и кромок, перемещение стрелками и миграция `20261025151403_AddEdgeSortOrder` описаны в [CATALOG_ORDER.md](catalog-api/docs/CATALOG_ORDER.md).

Контракты и правила описаны в [docs/EDGES_API.md](catalog-api/docs/EDGES_API.md). Миграция `20261022200130_AddEdgesCatalog` создаёт шесть отдельных таблиц кромок без тестовых данных. Миграцию применяет пользователь вручную; перед применением нужно проверить ожидающие миграции и целевую БД. Изменения не подключают прототип Front к API автоматически.

Сборка без обращения к NuGet: `dotnet build catalog-api/catalog-api.slnx --no-restore`. Зависимости для этого этапа не менялись.

## Локальный HTTPS

Catalog API использует общий development-сертификат из Core. Установка и проверка описаны в [core/DevelopmentCertificates/README.md](core/DevelopmentCertificates/README.md).

## Документация API

Настраиваемая выгрузка материалов и кромок в `.xlsx`: [контракт экспорта](catalog-api/docs/EXCEL_EXPORT_API.md).

`GET /api/for-admin/materials/compact` возвращает в поле `im` GUID миниатюры 240 px, при её отсутствии — 480 px, иначе null. Оригинал не подставляется. Список читает только GUID; файл можно лениво получить через публичный `GET /api/materials/images/{guid}` с годовым HTTP-кешем и ETag по GUID.

`ApiDoc:Enabled` включает OpenAPI, Scalar и Swagger UI. В `appsettings.json` документация выключена, в `appsettings.Development.json` включена. Если флаг отсутствует, документация недоступна. Для включения в другом окружении задайте `ApiDoc__Enabled=true` и перезапустите API.

## Подключения к БД и общая авторизация

`DbConnString` задаёт подключение к данным Catalog API. `DataProtectionDbConnString` задаёт отдельное подключение к общему хранилищу ключей Data Protection, которыми защищаются cookie авторизации; сами cookie в этой БД не хранятся. Строки подключения независимы: хост, база, пользователь и остальные параметры могут различаться.

Для общей авторизации с Accounts API должны совпадать хранилище ключей, имя приложения Data Protection (`Cookie.CookieName(Cookie.DomainName)`), имя cookie и её домен. `DataProtectionDbConnString` должен указывать на хранилище Accounts API, выдавшего cookie.

Оба подключения задаются в `appsettings.json` и `appsettings.Development.json` либо одноимёнными переменными окружения. После изменения строк подключения перезапустите API; перекомпиляция не требуется.

`Authentication:Provider` выбирает только провайдер БД ключей: `Accounts` — прежний PostgreSQL по умолчанию, `Vek` — MS SQL ВЕК. Cookie-авторизация и существующие проверки ролей остаются общими. ВЕК выпускает совместимую cookie; PostgreSQL с данными каталога остаётся отдельным подключением.

Catalog API использует `Microsoft.Data.SqlClient 7.1.1` без необязательного пакета Azure-аутентификации. `SatelliteResourceLanguages=en` исключает языковые сборки зависимостей из build/publish; русские сообщения самого API сохраняются. Публикацию выполняйте в чистую папку, чтобы в ней не оставались файлы предыдущих зависимостей.

## Публикация Main и Vek

Два профиля публикации в папку можно выбрать в IDE или запустить из корня репозитория:

```bash
dotnet publish catalog-api/Catalog.Api/Catalog.Api.csproj -c Release --no-restore -p:PublishProfile=Main
dotnet publish catalog-api/Catalog.Api/Catalog.Api.csproj -c Release --no-restore -p:PublishProfile=Vek
```

| Профиль | Папка результата | Источник опубликованного `appsettings.json` |
| --- | --- | --- |
| [Main](catalog-api/Catalog.Api/Properties/PublishProfiles/Main.pubxml) | `catalog-api/Catalog.Api/bin/Publish/Main/` | `appsettings.json` |
| [Vek](catalog-api/Catalog.Api/Properties/PublishProfiles/Vek.pubxml) | `catalog-api/Catalog.Api/bin/Publish/Vek/` | `appsettings.Vek.json` |

В папке `Vek` опубликованный `appsettings.json` содержит точную копию `appsettings.Vek.json`. Это полный конфиг, а не дополнение к основному. Редактируйте настройки ВЕК в этом JSON и повторяйте команду.

Окружение приложения остаётся `Production` по умолчанию; задавать `ASPNETCORE_ENVIRONMENT=Vek` не требуется. Исходные JSON и конфигурация локальной сборки не меняются. Обычный publish без профиля использует основной `appsettings.json`; `appsettings.Development.json` и отдельный `appsettings.Vek.json` не публикуются. При повторной публикации основной файл настроек перезаписывается выбранным вариантом.

## Диагностика локальной разработки

### Сборка и форматирование в sandbox

Если `dotnet build` или `dotnet format` зависает после запуска либо завершается ошибкой Roslyn `named pipe` / `Permission denied`, проверьте вывод и принадлежность процесса. Остановите только собственный зависший процесс и повторите команду через штатное разрешение на выполнение вне sandbox. Не останавливайте процессы пользователя и не меняйте постоянные порты или `hosts` ради проверки.

Обязательную проверку `dotnet format catalog-api/catalog-api.slnx --verify-no-changes --no-restore` выполняйте из корня репозитория. Если она обнаруживает ранее существовавшие нарушения вне задачи, отдельно проверьте изменённые файлы через `--include` и сообщите об ограничении общей проверки. Не правьте подмодуль `core` ради посторонних ошибок форматирования без предусмотренного правилами согласования.

### Ошибка PostgreSQL 42703 после переименования поля

Ошибка `42703` о недостающем переименованном столбце может означать, что приложение обновлено, а схема БД ещё нет. Сначала сверяйте подключение с нужным окружением и просматривайте историю через `dotnet ef migrations list --project catalog-api/Catalog.Database/Catalog.Database.csproj --context CatalogDbContext`. У design-time factory собственная строка подключения: одно значение `ASPNETCORE_ENVIRONMENT=Development` не гарантирует выбор нужной БД. Сравнивайте настройки без вывода секретов.

Применяйте миграции согласно `AGENTS.md`: по умолчанию это делает пользователь вручную. Агент может отступить от запрета только по явному разрешению пользователя для конкретных миграций и БД; перед применением проверьте весь список ожидающих миграций и задайте точную целевую миграцию. После применения проверьте историю и повторите затронутый запрос чтения. Не переписывайте исторические миграции и не создавайте недостающие столбцы вручную.

## Размеры материала

Материал ссылается на формат плиты через `sheetSizeId`. Поля `kvM` и `perimetrM` удалены из модели, административных запросов создания/обновления и ответов API. `GET /api/for-calculate/materials/{id}` возвращает только `{ "id", "width", "height" }`; ширина и высота берутся из формата плиты и выражены в целых миллиметрах.

Миграция `20261019214841_RemoveMaterialMeasurements` удаляет столбцы `kv_m` и `perimetr_m` из `catalog.materials`. Её применяет пользователь вручную после подготовки потребителей нового контракта. При откате миграция восстанавливает столбцы с нулями, а не прежние значения. До обновления модели БД создание материалов новой версией API не проверяется.