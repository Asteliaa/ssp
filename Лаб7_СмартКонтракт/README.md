# Лабораторная работа 7. Разработка смарт-контрактов на Solidity

Вся работа — один файл **`Lab7.sol`** (язык Solidity). Он запускается в браузерной среде **Remix IDE**, как в методичке; ничего устанавливать не нужно.

Контракты в `Lab7.sol`:

| Часть | Контракты | Что это |
|---|---|---|
| 1 | `StudentRegistry`, `ExtendedStudentRegistry` | образец из методички, код без изменений |
| 2 | `StudentRegistryPlus` | доп. задание: своя функция `getStudentsByMajor` |
| 3 | `BookRegistry`, `ExtendedBookRegistry` | **вариант 1** — справочник книг |
| 4 | `EmployeeRegistry`, `ExtendedEmployeeRegistry` | **вариант 2** — справочник сотрудников |

## Общий порядок работы в Remix IDE (по методичке)

1. Открыть <https://remix.ethereum.org>, **+ Create New File** → `Lab7.sol`, вставить весь код.
2. Скомпилировать: **Ctrl+S** или вкладка **Solidity Compiler** → **Compile** (версия 0.8.19 или выше). Должна появиться зеленая галочка.
3. Вкладка **Deploy & Run Transactions**:
   - ENVIRONMENT: **Remix VM (Cancun)** — встроенный эмулятор, 100 тестовых ETH на газ, после обновления страницы данные исчезают;
   - в списке **CONTRACT** выбрать нужный контракт (см. разделы ниже);
   - Gas Limit оставить автоматическим;
   - ввести параметры конструктора (строки — в кавычках) и нажать **Deploy**.
4. Раскрыть контракт в «Deployed Contracts». Оранжевые кнопки (**transact**) изменяют данные, синие (**call**) только читают.

---

## Часть 1. Образец из методички — `StudentRegistry`

1. CONTRACT: **StudentRegistry**, конструктор `"My University"` → **Deploy**.
2. `registerStudent`: `_name = "John Doe"`, `_age = 20`, `_major = "Computer Science"` → **transact**.
3. `getStudent` → `1` → **call** → `John Doe, 20, Computer Science, true`.
4. Сценарии из методички: зарегистрировать 3–4 студентов; `updateStudent`; `deleteStudent(1)`; `getAllStudents`; `getStudentCount`; сменить ACCOUNT на другой адрес и попробовать `registerStudent` → `Only admin can call this`; `getStudentByAddress`.
5. Наследование: **ExtendedStudentRegistry**, конструктор `"My University", "IT"`; `getStudentWithMajor(1, "Computer Science")` → `true`.

Замечание по коду методички: строка `studentIdByAddress[msg.sender] = studentCount;` записывает адрес админа (того, кто вызвал), а не студента, поэтому `getStudentByAddress` работает только для адреса админа и возвращает последнего зарегистрированного. Код оставлен как в методичке.

---

## Часть 2. Доп. задание — своя функция `getStudentsByMajor`

Контракт `StudentRegistryPlus` наследует `ExtendedStudentRegistry` из методички и добавляет функцию «найти всех студентов заданной специальности»:

```solidity
function getStudentsByMajor(string memory _major) public view returns (uint256[] memory) {
    bytes32 wanted = keccak256(bytes(_major));

    uint256 matches = 0;
    for (uint256 i = 1; i <= studentCount; i++) {
        if (students[i].isActive && keccak256(bytes(students[i].major)) == wanted) {
            matches++;
        }
    }

    uint256[] memory ids = new uint256[](matches);
    uint256 index = 0;
    for (uint256 i = 1; i <= studentCount; i++) {
        if (students[i].isActive && keccak256(bytes(students[i].major)) == wanted) {
            ids[index] = i;
            index++;
        }
    }
    return ids;
}
```

### Как она работает

1. **Вход** — специальность, например `"Computer Science"`. `memory` — строка хранится временно, только на время вызова.
2. **`public view`** — вызывается снаружи и только *читает* данные. В Remix это кнопка **call**: транзакции нет, газ не тратится (как у `getStudent`).
3. **Сравнение строк через хэш.** В Solidity строки нельзя сравнить оператором `==`. Обе строки переводятся в байты (`bytes(...)`), от них берется хэш `keccak256`; одинаковые строки дают одинаковые хэши. Хэш искомой специальности считается один раз (`wanted`).
4. **Первый цикл** проходит номера `1 … studentCount` и считает подходящих: студент действующий (`isActive`, не удален) **и** хэш его специальности совпадает. Подсчет нужен, потому что длину массива в памяти задают при создании — «дорастить» его нельзя.
5. **Второй цикл** создает массив нужной длины (`new uint256[](matches)`) и записывает в него номера найденных студентов; `index` — позиция следующей записи.
6. **Результат** — массив номеров; если никого нет — пустой массив. По номеру данные берутся через `getStudent(id)`.

Прием тот же, что в `getAllStudents` из методички (два прохода и массив в памяти). Сравнение чувствительно к регистру (`"Computer Science"` ≠ `"computer science"`). Цикл по всем студентам расходует газ пропорционально их числу — снаружи через **call** это бесплатно, но внутри транзакции при большом списке дорого (методичка предупреждает про циклы и газ).

### Демонстрация

1. CONTRACT: **StudentRegistryPlus**, конструктор `"My University", "IT"` → **Deploy**.
2. `registerStudent` → **transact** четыре раза: `"Anna", 20, "Computer Science"`; `"Boris", 21, "Mathematics"`; `"Clara", 22, "Computer Science"`; `"Dmitry", 23, "Computer Science"`.
3. `getStudentsByMajor("Computer Science")` → **call** → `[1, 3, 4]`; `"Mathematics"` → `[2]`; `"Biology"` → `[]`.
4. `deleteStudent(3)` → снова `"Computer Science"` → `[1, 4]` (удаленный не учитывается).
5. `updateStudent(2, "Boris", 21, "Computer Science")` → для `"Computer Science"` → `[1, 2, 4]`.

---

## Часть 3. Вариант 1 — справочник книг (`BookRegistry`)

**Задание:** контракт по книгам — название, автор, год издания, наличие (дополнительно — жанр).

| Элемент | Что это |
|---|---|
| `struct Book` | `title`, `author`, `year`, `available`, `genre`, `exists` (не удалена) |
| `mapping(uint256 => Book) books` | хранилище, ключ — номер книги 1, 2, 3… |
| `admin`, `libraryName`, `bookCount` | админ (кто задеплоил), название библиотеки, последний выданный номер |
| `onlyAdmin`, `bookExists(id)` | модификаторы: менять может только админ; книга должна существовать |
| `require` | название и автор не пустые, год 1…2100 |
| события | `BookAdded`, `BookUpdated`, `BookDeleted`, `AvailabilityChanged`, `AdminChanged` |
| `ExtendedBookRegistry` | наследование: поле `branch` (филиал), функция `isGenre` |

Функции, меняющие данные (только админ, **transact**): `addBook(title, author, year, available, genre)`, `updateBook(id, title, author, year, genre)`, `setAvailability(id, available)`, `deleteBook(id)` (мягкое удаление, номер не переиспользуется), `changeAdmin`, `setLibraryName`.

Чтение (**call**, доступно всем): `getBook(id)`, `getAllBooks()` (массивы `ids, titles, authors, yearList, availability`), `getBookCount()`, `getAvailableBookIds()`, `findByAuthor(author)`, геттеры `books(id)`, `admin()`, `libraryName()`, `bookCount()`.

### Демонстрация

1. CONTRACT: **BookRegistry**, конструктор `"City Library"` → **Deploy**.
2. `addBook` → **transact** три раза:

   | title | author | year | available | genre |
   |---|---|---|---|---|
   | `"War and Peace"` | `"L. Tolstoy"` | `1869` | `true` | `"Novel"` |
   | `"Anna Karenina"` | `"L. Tolstoy"` | `1877` | `false` | `"Novel"` |
   | `"Master and Margarita"` | `"M. Bulgakov"` | `1967` | `true` | `"Novel"` |

3. `getBook(1)` → `War and Peace, L. Tolstoy, 1869, true, Novel`; `getBookCount` → `3`; `getAllBooks` → три книги; `getAvailableBookIds` → `[1, 3]`; `findByAuthor("L. Tolstoy")` → `[1, 2]`.
4. `setAvailability(2, true)` → `getAvailableBookIds` → `[1, 2, 3]`. `updateBook(3, "The Master and Margarita", "M. Bulgakov", 1967, "Fantasy")` → `getBook(3)`.
5. `deleteBook(1)` → `getBookCount` = `2`; `getBook(1)` → ошибка `Book does not exist`.
6. Другой ACCOUNT → `addBook` → `Only admin can call this`.
7. Валидация: `addBook("", "A", 2000, true, "")` → `Title cannot be empty`; год `0` или `2101` → `Invalid year`.
8. Наследование: **ExtendedBookRegistry**, конструктор `"Uni Library", "Branch 1"`; добавить книгу, `isGenre(1, "Novel")`, `branch()`.

Имя массива лет — `yearList`: слово `years` в Solidity зарезервировано (единица времени) и не компилируется.

---

## Часть 4. Вариант 2 — справочник сотрудников (`EmployeeRegistry`)

**Задание:** справочник по сотрудникам фирмы — фамилия, имя, год рождения, должность, стаж, оклад.

| Элемент | Что это |
|---|---|
| `struct Employee` | `lastName`, `firstName`, `birthYear`, `position`, `experience` (лет), `salary`, `isActive` |
| `mapping(uint256 => Employee) employees` | хранилище, ключ — номер сотрудника |
| `admin`, `companyName`, `employeeCount` | админ, название фирмы, последний выданный номер |
| `onlyAdmin`, `employeeExists(id)` | модификаторы |
| `require` | фамилия, имя, должность не пустые; год рождения 1900…2100; стаж ≤ 80; оклад > 0 |
| события | `EmployeeAdded`, `EmployeeUpdated`, `EmployeeFired`, `SalaryChanged`, `AdminChanged` |
| `ExtendedEmployeeRegistry` | наследование: поле `department` (отдел), функция `hasPosition` |

Оклад — число в условных единицах; деньги (ether) контракт не принимает и не переводит.

Функции, меняющие данные (только админ, **transact**): `addEmployee(lastName, firstName, birthYear, position, experience, salary)`, `updateEmployee(id, …)`, `setSalary(id, salary)`, `raiseSalary(id, percent)` (1…100 %), `setPosition(id, position)`, `fireEmployee(id)` (мягкое удаление), `changeAdmin`, `setCompanyName`.

Чтение (**call**): `getEmployee(id)`, `getAllEmployees()`, `getEmployeeCount()`, `totalPayroll()` (сумма окладов), `averageSalary()` (средний оклад, 0 при пустом списке), `findByPosition(position)`, `findByMinExperience(years)`, геттеры `employees(id)`, `admin()`, `companyName()`, `employeeCount()`.

### Демонстрация

1. CONTRACT: **EmployeeRegistry**, конструктор `"ACME Ltd"` → **Deploy**.
2. `addEmployee` → **transact** три раза:

   | lastName | firstName | birthYear | position | experience | salary |
   |---|---|---|---|---|---|
   | `"Ivanov"` | `"Ivan"` | `1985` | `"Programmer"` | `10` | `3000` |
   | `"Petrov"` | `"Petr"` | `1990` | `"Programmer"` | `5` | `2000` |
   | `"Sidorova"` | `"Anna"` | `1978` | `"Director"` | `20` | `5000` |

3. `getEmployee(1)` → `Ivanov, Ivan, 1985, Programmer, 10, 3000`; `getEmployeeCount` → `3`; `totalPayroll` → `10000`; `averageSalary` → `3333`; `findByPosition("Programmer")` → `[1, 2]`; `findByMinExperience(10)` → `[1, 3]`.
4. `raiseSalary(1, 10)` → оклад `3300`; `setSalary(2, 2500)`; `setPosition(2, "Architect")`; `totalPayroll` → `10800`. `updateEmployee(3, "Sidorova", "Anna", 1978, "CEO", 21, 5500)`.
5. `fireEmployee(1)` → `getEmployeeCount` = `2`; `getEmployee(1)` → `Employee does not exist`.
6. Другой ACCOUNT → `addEmployee` / `setSalary` → `Only admin can call this`.
7. Валидация: пустая фамилия → `Last name cannot be empty`; `salary = 0` → `Salary must be positive`; `birthYear = 1850` → `Invalid birth year`; `raiseSalary(1, 150)` → `Percent must be 1..100`.
8. Наследование: **ExtendedEmployeeRegistry**, конструктор `"ACME", "IT"`; добавить сотрудника, `hasPosition(1, "Programmer")`, `department()`.

