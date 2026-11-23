# Лабораторная работа 7. Разработка смарт-контрактов на Solidity

Проект устроен как в методичке: каждый контракт лежит в своем файле `.sol`, файлы запускаются в браузерной среде **Remix IDE**, ничего устанавливать не нужно.

| Файл | Контракты | Что это |
|---|---|---|
| `StudentRegistry.sol` | `StudentRegistry`, `ExtendedStudentRegistry` | образец из методички, код без изменений |
| | `StudentRegistryPlus` | доп. задание: своя функция `getStudentsByMajor` |
| `BookRegistry.sol` | `BookRegistry`, `ExtendedBookRegistry` | **вариант 1**: справочник книг |
| `EmployeeRegistry.sol` | `EmployeeRegistry`, `ExtendedEmployeeRegistry` | **вариант 2**: справочник сотрудников |

Контракты вариантов написаны по образцу `StudentRegistry`. В них те же разделы в том же порядке: DATA STRUCTURES, STATE VARIABLES, EVENTS, MODIFIERS, CONSTRUCTOR, CORE FUNCTIONS, VIEW FUNCTIONS (READ ONLY), ADMIN FUNCTIONS и в конце EXTRA с наследником.

## Порядок работы в Remix IDE (по методичке)

1. Открыть <https://remix.ethereum.org>.
2. Кнопкой **+ Create New File** создать файлы `StudentRegistry.sol`, `BookRegistry.sol`, `EmployeeRegistry.sol` и вставить в них код.
3. Открыть нужный файл и скомпилировать: **Ctrl+S** или **Solidity Compiler → Compile**, версия 0.8.19. Должна появиться зеленая галочка.
4. На вкладке **Deploy & Run Transactions**:
   - выбрать Environment **Remix VM**;
   - в списке **Contract** выбрать контракт;
   - ввести параметр конструктора;
   - нажать **Deploy**.
5. Раскрыть контракт в **Deployed Contracts**:
   - оранжевые кнопки (**Transact**) изменяют данные;
   - синие кнопки (**Call**) только читают.

Пошаговый сценарий показа описан в [`Сдача_лабы.md`](Сдача_лабы.md).

## Вариант 1: `BookRegistry`

Поля: название, автор, год издания, наличие, жанр (жанр добавлен сверх задания, в методичке написано «можно что-либо еще»).

| Функция | Тип | Назначение |
|---|---|---|
| `addBook`, `updateBook`, `deleteBook` | Transact | добавить, изменить, удалить (мягко: `isActive = false`) |
| `setAvailability` | Transact | отметить наличие или выдачу книги |
| `getBook`, `getAllBooks`, `getBookCount` | Call | чтение одной книги, всех книг, числа книг |
| `getAvailableBookIds`, `findByAuthor` | Call | номера книг в наличии и книг автора |
| `changeAdmin`, `setLibraryName` | Transact | функции администратора |
| `ExtendedBookRegistry`: `branch`, `setBranch`, `getBookWithGenre` | | наследник |

## Вариант 2: `EmployeeRegistry`

Поля: фамилия, имя, год рождения, должность, стаж, оклад.

| Функция | Тип | Назначение |
|---|---|---|
| `addEmployee`, `updateEmployee`, `fireEmployee` | Transact | принять, изменить, уволить |
| `setSalary`, `raiseSalary`, `setPosition` | Transact | оклад, повышение на 1–100 %, должность |
| `getEmployee`, `getAllEmployees`, `getEmployeeCount` | Call | чтение |
| `totalPayroll`, `averageSalary` | Call | фонд оплаты труда и средний оклад |
| `findByPosition`, `findByMinExperience` | Call | поиск по должности и стажу |
| `changeAdmin`, `setCompanyName` | Transact | функции администратора |
| `ExtendedEmployeeRegistry`: `department`, `setDepartment`, `getEmployeeWithPosition` | | наследник |

## Доп. задание: `getStudentsByMajor`

Функция находится в контракте `StudentRegistryPlus` в конце файла `StudentRegistry.sol` и возвращает номера активных студентов заданной специальности. Строки сравниваются по хэшу `keccak256`. Первый цикл считает подходящих студентов, второй заполняет массив, как в `getAllStudents` из методички.

## Отчет

`Лаб7_СмартКонтракт_Отчет.docx` / `.pdf`: отчет по СТП БГУИР 01-2024 на 25 страниц.
