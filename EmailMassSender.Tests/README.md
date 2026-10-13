# Unit-тесты EmailMassSender

Проект `EmailMassSender.Tests` содержит unit-тесты приложения рассылки писем
`EmailMassSender` (`ems`).

Полный список тест-кейсов с описанием входных данных и ожидаемых результатов —
в файле [TEST-CASES.md](TEST-CASES.md).

## Требования

- .NET 10 SDK.
- Доступ к nuget.org для восстановления пакетов.

Реальный SMTP-сервер и доступ в интернет для тестов не нужны.

## Запуск

Из корня репозитория:

```bat
> dotnet test --solution EmailMassSenderSln.sln
```

Запуск одного класса или одного теста (фильтры xUnit v3):

```bat
> dotnet test --solution EmailMassSenderSln.sln -- --filter-class EmailMassSender.Tests.EmailMassSendingHostRecyclerTests
> dotnet test --solution EmailMassSenderSln.sln -- --filter-method *S26_*
```

Тесты также запускаются из Visual Studio, Rider и VS Code (C# Dev Kit) через
Test Explorer.

### Почему нужен `global.json`

Тесты написаны на **xUnit v3**, который работает на платформе
**Microsoft.Testing.Platform (MTP)**. Начиная с .NET 10 SDK команда `dotnet test`
запускает MTP-проекты только при явном включении в `global.json` в корне
репозитория:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Без этого файла `dotnet test` завершится ошибкой
`Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK`.

Новый `dotnet test` принимает путь к решению через `--solution`, а аргументы для
самих тестов передаются после `--`.

## Структура проекта

| Файл | Назначение |
|------|------------|
| `EmailMassSendingHostRecyclerTests.cs` | Тесты R01–R08: повторный запуск обработки задач (`EmailMassSendingHostRecycler`) |
| `EmailMassSendingServiceTests.cs` | Тесты S01–S22: конструктор сервиса, подготовка и обработка задач с выключенным SMTP |
| `EmailMassSendingServiceSmtpTests.cs` | Тесты S23–S27: отправка через фейковый SMTP-сервер, ошибки, повторы, отмена |
| `Infrastructure/TestWorkspace.cs` | Временная директория с текстом письма и файлами задач, фабрика конфигурации |
| `Infrastructure/FakeSmtpServer.cs` | Минимальный локальный SMTP-сервер для тестов |
| `TEST-CASES.md` | Описание тест-кейсов |

Имя каждого тестового метода начинается с идентификатора кейса из
`TEST-CASES.md` (например, `S26_Rejected_RetriedUntilAttemptsExhausted`).

## Вспомогательные классы

### TestWorkspace

Создаёт для каждого теста отдельную директорию
`%TEMP%\ems-tests\<guid>` и удаляет её после завершения теста. Поэтому тесты
не мешают друг другу и могут выполняться параллельно.

Основные методы:

- `CreateConfiguration()` — конфигурация по умолчанию: один поток, SMTP
  выключен, задачи хранятся во временной директории;
- `CreateGroup(params string[] receivers)` — группа рассылки с текстом из
  временного файла;
- `GetTaskFilePath(groupId, group)` — путь к файлу задачи
  `<группа>_<MD5 текста>.task`, вычисленный так же, как в приложении;
- `ReadTask(...)` / `WriteTask(...)` — чтение и подготовка файла задачи. Через
  `WriteTask` создаются задачи с нужным состоянием элементов (уже отправлен,
  попытки исчерпаны, ожидание повтора и т. д.);
- `GetTaskFiles()` — список файлов задач.

### FakeSmtpServer

Слушает случайный свободный порт на `127.0.0.1` и поддерживает минимальный
набор команд SMTP (`EHLO`/`HELO`, `MAIL FROM`, `RCPT TO`, `DATA`, `QUIT`) без TLS
и без авторизации. Поэтому в тестах указывается `UseSsl = false`.

Режимы работы (`FakeSmtpMode`):

| Режим | Поведение | Где используется |
|-------|-----------|------------------|
| `Accept` | Принимает все письма и сохраняет их в `Mails` | S23, S24 |
| `Reject` | Сразу закрывает соединение — клиент получает ошибку отправки | S25, S26 |
| `Hang` | Принимает соединение и не отвечает | S27 (проверка отмены) |

Свойство `Connections` — количество подключений, то есть попыток отправки.

## Доступ к внутренним классам

`EmailMassSendingHostRecycler` объявлен как `internal`. Чтобы тесты могли его
использовать, в `EmailMassSender.csproj` добавлено:

```xml
<InternalsVisibleTo Include="EmailMassSender.Tests" />
```

## Регрессионные тесты

Тест **S26** проверяет исправленную ошибку повторной отправки. Раньше письмо,
которое ждало следующей попытки (`WaitFor` в будущем), считалось обработанным.
Задача получала статус SUCCESS, повторный запуск (`HostRecycle`) не выполнялся,
и после первой ошибки письмо больше не отправлялось. Тест проверено: без
исправления он падает (выполняется 1 попытка вместо 3).

## Что не покрыто

- `Program.Main` — загрузка `appsettings.json`, `usersettings.json` и аргументов
  командной строки. Метод работает с текущей директорией и консолью; для
  тестирования загрузку конфигурации нужно вынести в отдельный класс.
- Отправка через реальный SMTP с TLS и авторизацией.
