# Настройка окружения для лабораторной работы №1 (IL / JIT)

Пошаговая инструкция по установке всего необходимого софта на Windows.
Все команды выполняются в PowerShell.

## 1. Что нужно установить

| Компонент | Назначение | Обязательно |
|---|---|---|
| .NET 8.0 SDK | компиляция и запуск C#-проектов | да |
| Git | клонирование репозитория, коммиты | да |
| Visual Studio 2022 или VS Code + расширение C# Dev Kit | редактирование и отладка кода | да (на выбор) |
| ilspycmd (или ildasm.exe) | просмотр IL-кода в текстовом виде для сверки со скриншотами | нет, по желанию |
| BenchmarkDotNet | измерение производительности (Задание 4) | ставится автоматически через NuGet |

## 2. Установка .NET 8 SDK

### Вариант А — через winget (рекомендуется)

```powershell
winget install -e --id Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
```

### Вариант Б — вручную

Скачать установщик с официальной страницы `https://dotnet.microsoft.com/download/dotnet/8.0`
(раздел "SDK", колонка Windows x64) и запустить `.exe`.

### Проверка установки

Откройте новое окно терминала (PATH обновляется только в новых сессиях) и выполните:

```powershell
dotnet --version
```

Ожидаемый вывод: `8.0.x`. Если команда не найдена — перезапустите терминал/IDE
или перезагрузите компьютер (winget иногда требует перезапуска explorer.exe
для обновления переменной PATH).

Дополнительно можно проверить список установленных SDK:

```powershell
dotnet --list-sdks
```

## 3. Установка Git

```powershell
winget install -e --id Git.Git --accept-package-agreements --accept-source-agreements
```

Проверка:

```powershell
git --version
```

Настройка имени и почты для коммитов (один раз на компьютер):

```powershell
git config --global user.name "Ваше Имя"
git config --global user.email "you@example.com"
```

## 4. Установка среды разработки

### Вариант А — Visual Studio 2022 (полноценная IDE, рекомендуется для этой лабораторной)

```powershell
winget install -e --id Microsoft.VisualStudio.2022.Community --accept-package-agreements --accept-source-agreements
```

При установке в списке workload'ов отметить **".NET desktop development"**.

### Вариант Б — VS Code (лёгкий редактор)

```powershell
winget install -e --id Microsoft.VisualStudioCode --accept-package-agreements --accept-source-agreements
```

После установки открыть VS Code → Extensions → установить **C# Dev Kit** (от Microsoft).

## 5. Клонирование репозитория

```powershell
git clone https://github.com/KsushaKhadzhinova/ssp.git
cd ssp\Лаб1_IL_JIT
```

## 6. Сборка и запуск проектов

Репозиторий содержит три проекта:

- **IlAnalysis** — Задания 1–3 (анализ IL-кода, ILGenerator, DynamicMethod);
- **JitResearch** — Задания 4–5 (влияние JIT-компиляции на производительность);
- **Tests** — модульные тесты (xUnit) на код обоих проектов выше.

### 6.1 IlAnalysis (Задания 1–3)

```powershell
cd IlAnalysis
dotnet run
```

**Важно:** проект должен собираться в конфигурации **Debug** (она используется
по умолчанию у `dotnet run`). В Release-сборке `MethodBody.GetMethodBody()`
может вернуть `null`, так как JIT/компилятор вправе не сохранять отладочные
метаданные IL для оптимизированного кода.

### 6.2 JitResearch (Задания 4–5)

Восстановление NuGet-пакета BenchmarkDotNet происходит автоматически при
первой сборке:

```powershell
cd ..\JitResearch
dotnet run
```

Для полноценного бенчмарка через BenchmarkDotNet (Задание 4, дополнительно)
нужна конфигурация **Release** — иначе библиотека откажется запускаться,
предупредив о недостоверности замеров в неоптимизированной сборке:

```powershell
dotnet run -c Release -- --bench
```

Прогон занимает 1–3 минуты: BenchmarkDotNet сам делает разогрев (warm-up)
и несколько итераций замеров для статистической точности. Результаты
появятся в консоли и дополнительно сохранятся в
`JitResearch\BenchmarkDotNet.Artifacts\results\`.

### 6.3 Tests — модульные тесты

Более 70 тестов (xUnit), проверяющих корректность всех заданий: результаты
факториала, максимума из трёх чисел, абсолютной величины, соответствие
IL-байтов ожидаемым, суммирование массива, поведение виртуального вызова
и т. д. Тесты не зависят от IDE — запускаются из командной строки:

```powershell
cd ..\Tests
dotnet test
```

Ожидаемый результат — `Passed! - Failed: 0, Passed: 70, Skipped: 0`.
Для подробного постатейного вывода добавьте `--logger "console;verbosity=detailed"`.

## 7. Просмотр IL-кода в текстовом виде (по желанию)

Методичка упоминает классические `ildasm`/`ilasm` — это инструменты
.NET Framework, которые не входят в кроссплатформенный .NET SDK.
Современный аналог, работающий поверх .NET SDK:

```powershell
dotnet tool install -g ilspycmd
cd IlAnalysis
dotnet build
ilspycmd -il bin\Debug\net8.0\IlAnalysis.dll
```

Команда выведет полный листинг IL-кода сборки — удобно сверять с тем,
что программа печатает в HEX через рефлексию.

## 8. Типичные проблемы

| Симптом | Причина | Решение |
|---|---|---|
| `dotnet: команда не найдена` | PATH не обновился | открыть новое окно терминала / перезагрузить компьютер |
| `GetMethodBody() вернул null` | сборка в Release | запускать `dotnet run` без `-c Release` |
| BenchmarkDotNet пишет `Unable to find 8.0 target framework` | не хватает SDK | проверить `dotnet --list-sdks`, переустановить SDK |
| Ошибка `NETSDK1045` (SDK version is too old) | установлена SDK ниже 8.0 | обновить через `winget upgrade Microsoft.DotNet.SDK.8` |
