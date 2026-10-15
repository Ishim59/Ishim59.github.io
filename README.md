# Бардымская гимназия

Сайт состоит из независимой статической части и API на C# / ASP.NET Core 10 с PostgreSQL 17.
GitHub Pages обслуживает статическую часть. API запускается отдельно: локально, в Docker или на сервере с .NET.

## Где редактировать

```text
content/
  home.html                 Содержимое главной страницы
  site.json                 Меню, вкладки и адрес API
  pages.json                Каталог страниц и старых адресов
  pages/documents.html      Текст раздела документов
  pages/schedule.html       Текст раздела расписания
  news.json                 Начальные новости статического сайта
src/
  templates/                Общие шапка, подвал и окно поиска
  components/layout.js      Генерация страниц с общей разметкой
  client/components/        Классы меню, поиска, вкладок, новостной ленты
  client/main.js            Инициализация компонентов
  styles/                   Исходники стилей по назначению
scripts/
  build.mjs                 Сборка сайта
  check-links.mjs           Проверка внутренних ссылок
backend/
  Gymnasium.Api/
    Controllers/            HTTP-запросы и статусы ответов
    Contracts/              DTO и проверка входных данных
    Models/                 Модель новости
    Services/               Логика новостей и каталог страниц
    Repositories/           Запросы к PostgreSQL через EF Core
    Data/                   DbContext и начальные данные
    Migrations/             Версионированная схема PostgreSQL
    Authentication/         Защита операций изменения новостей
  Gymnasium.Api.Tests/      Интеграционные тесты с PostgreSQL
```

Корневой `index.html`, каталоги `documents/`, `schedule/` и прочие читаемые адреса — результат сборки.
Их сохраняем в Git, чтобы существующая настройка GitHub Pages `main / (root)` продолжала работать.
Менять нужно `src/` и `content/`, затем запускать сборку. Старые адреса `pages/index-*.html`
остаются короткими переадресациями для опубликованных ссылок и закладок.

`assets/imported/` — исходный перенесённый архив документов и фотографий. Идентификаторы файлов
не меняем, чтобы сохранить все ссылки. Новые документы можно добавлять с понятными именами,
например `assets/documents/admission-rules-2027.pdf`.

## Статический сайт и GitHub Pages

Нужен Node.js 22 или новее.

```sh
npm ci
npm run build
npm test
npm run check
```

После изменения исходников включите в коммит и исходники, и результаты сборки.
В GitHub Desktop сделайте Commit и Push. Настройку Pages `main / (root)` менять не нужно.
CI проверяет сборку, ссылки, форматирование и C# API, но сам не публикует сервер.

```sh
npm run format
npm run build
```

Адреса: `/documents/`, `/schedule/`, `/teachers/`, `/admissions/`, `/news/`.

## C# API с локальным PostgreSQL

Нужны .NET SDK 10 и PostgreSQL 17. Создайте **отдельную** базу `gymnasium` и пользователя с доступом к ней.
Скопируйте `backend/Gymnasium.Api/appsettings.Local.example.json` в
`backend/Gymnasium.Api/appsettings.Local.json` и укажите подключение.
`appsettings.Local.json` исключён из Git. Пароли нельзя помещать в `appsettings.json`.

Альтернатива конфигурационному файлу — переменные окружения:

```text
ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=gymnasium;Username=gymnasium;Password=YOUR_PASSWORD
Admin__ApiKey=YOUR_RANDOM_SECRET_AT_LEAST_32_CHARACTERS
```

Затем из корня репозитория:

```sh
dotnet tool restore
dotnet run --project backend/Gymnasium.Api -- --migrate
dotnet run --project backend/Gymnasium.Api -- --seed
npm run build:server
dotnet run --project backend/Gymnasium.Api
```

Сайт и API доступны на `http://localhost:5080`. Результат серверной сборки находится в `dist/`,
который не попадает в Git. В нём `/news/` получает актуальную ленту из API.
Главная страница и сведения об организации по-прежнему собираются из `content/`.

Миграции применяются отдельной командой `--migrate`. Начальные три новости загружаются явно через `--seed`;
удалённые новости не восстанавливаются автоматически при каждом перезапуске.

Для новой миграции после изменения модели:

```sh
dotnet ef migrations add DescribeYourChange --project backend/Gymnasium.Api
```

## API

| Запрос | Назначение | Доступ |
| --- | --- | --- |
| `GET /api/news?limit=50` | Лента новостей | Публичный |
| `GET /api/news/{slug}` | Полная новость | Публичный |
| `POST /api/news` | Создать новость | `X-Admin-Key` |
| `PUT /api/news/{slug}` | Изменить новость | `X-Admin-Key` |
| `DELETE /api/news/{slug}` | Удалить новость | `X-Admin-Key` |
| `GET /api/pages?query=...` | Поиск разделов по заголовкам | Публичный |
| `GET /api/pages/{slug}` | Содержание раздела | Публичный |
| `GET /api/health` | Жив ли процесс | Публичный |
| `GET /api/health/ready` | Доступен ли PostgreSQL | Публичный |

Пример тела `POST`/`PUT`:

```json
{
  "slug": "new-school-project",
  "title": "Новый школьный проект",
  "summary": "Краткое описание",
  "content": "Полный текст новости. Передавайте обычный текст, не HTML.",
  "publishedAt": "2026-10-15"
}
```

Без ключа или с ключом короче 32 символов операции записи недоступны. Ключ нельзя включать в браузерный код.
Входной текст экранируется перед отображением. Повторный slug возвращает 409, неверные поля — 400.
Для операций записи действует ограничение частоты. Это минимальный API для одного администратора;
веб-панели, учётных записей педагогов и загрузки новых файлов через API пока нет.

## Docker Compose

Скопируйте `.env.example` в `.env`, задайте собственные `POSTGRES_PASSWORD` и `ADMIN_API_KEY`.

```sh
docker compose up --build -d
docker compose exec api dotnet Gymnasium.Api.dll --seed
```

Адрес: `http://localhost:5080`. PostgreSQL доступен локально на порту 5433, чтобы не конфликтовать
с установленной базой на 5432. Данные хранятся в Docker volume `postgres-data`.
Миграции в этом режиме применяются при запуске API. Пароли не включены в образ.

## Проверки

```sh
npm test
npm run check
dotnet test Gymnasium.sln
```

Тесты API используют настоящую PostgreSQL и выполняются только при заданной переменной `TEST_POSTGRES`,
указывающей на **отдельную тестовую базу**. Без неё интеграционные тесты помечены как пропущенные.
Проверяются авторизация, CRUD, валидация, экранирование HTML, сохранение данных после перезапуска и готовность БД.

## Публикация API

GitHub Pages не запускает ASP.NET Core или PostgreSQL. API и базу нужно разместить отдельно.
После размещения укажите адрес вида `https://api.example.org/api` в `content/site.json` → `apiBaseUrl`
и пересоберите сайт. В `Cors:AllowedOrigins` разрешён `https://ishim59.github.io`;
при смене домена измените этот список на сервере. Для публичного API необходим HTTPS на хостинге/reverse proxy.
Всё, что не обращается к API, продолжит работать как обычный статический сайт.

Проверено: сборка .NET 10, миграция на отдельном PostgreSQL 17, четыре интеграционных теста API, два теста шаблонов и ссылки статического сайта. Docker Compose подготовлен, но локально не запускался: Docker на этой машине не установлен.
