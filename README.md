# mm-catalog-api

ASP.NET Core API на .NET 10, PostgreSQL и EF Core. Обслуживает Catalog Front,
публичный каталог, Calculate API и личный кабинет ВЕК.

## Локальный запуск

Требуются доступная БД `mm-debug`, восстановленные зависимости и общий
[development-сертификат](core/DevelopmentCertificates/README.md).
Из корня репозитория:

```bash
dotnet build catalog-api/catalog-api.slnx --no-restore
Cookie__DomainName=.debug.feach.ru dotnet run --project catalog-api/Catalog.Api/Catalog.Api.csproj --launch-profile Debug --no-build --no-restore --urls https://catalog-api.debug.feach.ru:5005
```

Команда использует локальный адрес Catalog Front и общий домен cookie с Accounts API.
Без переопределения `--urls` профиль `Debug` слушает порт `5015`.
Перед запуском проверьте занятость порта; постоянные профили и `hosts` для разовой проверки не меняйте.
`dotnet restore catalog-api/catalog-api.slnx` нужен при первой подготовке или изменении зависимостей.

## Настройка и интеграции

Настройки находятся в `catalog-api/Catalog.Api/appsettings*.json`; вложенные ключи
переопределяются переменными окружения с `__`. После изменения настроек API нужно перезапустить.

| Настройка | Назначение |
| --- | --- |
| `DbConnString` | PostgreSQL с данными каталога |
| `DataProtectionDbConnString` | Общее хранилище ключей авторизации |
| `Cookie.CookieName`, `Cookie.DomainName` | Имя и домен cookie |
| `Authentication.Provider` | `Accounts` — ключи в PostgreSQL; `Vek` — ключи в MS SQL |
| `Cors.DomainName` | Разрешённый домен и его поддомены |
| `CalculateApi.BaseAddress`, `CalculateApi.MaterialPricePercentagesPath` | Получение наценок из Calculate API |
| `ApiDoc.Enabled` | OpenAPI, Scalar и Swagger UI |
| `StroydvorPriceSync` | Включение фоновой синхронизации, cron и часовой пояс |

Для общей авторизации должны совпадать хранилище ключей, имя cookie, её домен и имя
приложения Data Protection: `CookieName(DomainName)`. Данные каталога и ключи используют
независимые подключения; для локальной разработки оба должны указывать на `mm-debug`.
Провайдер `Vek` меняет только хранилище ключей, данные каталога остаются в PostgreSQL.
CORS дополнительно разрешает точный мобильный origin `capacitor://localhost`.

В Development Calculate API ожидается на `https://calculate-api.debug.feach.ru:5006/`.
Фоновая синхронизация Стройдвора управляется `StroydvorPriceSync:Enabled`; локально она отключена.
Не включайте её в проверках, которым не требуется обращение к поставщику.

Документация API включена в Development: `/scalar`, `/swagger` и `/swagger/v1/swagger.json`.
В других окружениях её можно включить через `ApiDoc__Enabled=true`.

## База данных

Миграции находятся в `catalog-api/Catalog.Database/Migrations`; автоматически при старте
они не применяются. Перед обновлением сверяйте целевую БД и ожидающие миграции;
изменения схемы и контрактов разворачивайте согласованно с потребителями API.
Правила применения миграций — в [AGENTS.md](AGENTS.md).

Design-time factory имеет собственное подключение по умолчанию: одного
`ASPNETCORE_ENVIRONMENT=Development` недостаточно для выбора тестовой БД.
Перед локальными EF-командами задавайте `DbConnString` с `Database=mm-debug`.

Поиск использует расширение PostgreSQL `pg_trgm` и GIN-индекс.
При старте API заполняет пустые поисковые тексты пакетами; после прерывания продолжает работу.
Для полной пересборки поисковых текстов запустите API с `MaterialSearch__Rebuild=true`,
а после завершения уберите параметр.

## Публикация

```bash
dotnet publish catalog-api/Catalog.Api/Catalog.Api.csproj -c Release --no-restore -p:PublishProfile=Main
dotnet publish catalog-api/Catalog.Api/Catalog.Api.csproj -c Release --no-restore -p:PublishProfile=Vek
```

| Профиль | Результат | Источник опубликованного `appsettings.json` |
| --- | --- | --- |
| [Main](catalog-api/Catalog.Api/Properties/PublishProfiles/Main.pubxml) | `catalog-api/Catalog.Api/bin/Publish/Main/` | `appsettings.json` |
| [Vek](catalog-api/Catalog.Api/Properties/PublishProfiles/Vek.pubxml) | `catalog-api/Catalog.Api/bin/Publish/Vek/` | `appsettings.Vek.json` |

Конфигурация ВЕК — полный файл, а не дополнение к основному. Окружение остаётся
`Production`; задавать `ASPNETCORE_ENVIRONMENT=Vek` не нужно. Development-конфигурация
и отдельный `appsettings.Vek.json` не публикуются. Публикуйте в чистую папку.

## Структура и проверки

- `catalog-api/Catalog.Api/Features` — вертикальные срезы, контракты, валидация и бизнес-логика.
- `catalog-api/Catalog.Api/Controllers` — HTTP-маршруты и привязка запросов.
- `catalog-api/Catalog.Database` — сущности, EF-конфигурация и миграции.
- `core` — общий подмодуль авторизации, HTTP, ошибок, документации и сертификатов.

Подробности поведения ищите в коде и сгенерированном OpenAPI.
Проверки запускайте только для затронутого поведения; полный регресс — по отдельному запросу.
Сценарии и требования к окружению — в [TEST_CASES.md](catalog-api/docs/TEST_CASES.md),
HTTP-раннер — `catalog-api/http_regression_runner.py`. Учётные данные DEV находятся в
`../mm-accounts-api/accounts-api/docs/TEST_CREDENTIALS.md`.