// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

// Лабораторная работа 7. Все контракты в одном файле:
//   ЧАСТЬ 1. StudentRegistry, ExtendedStudentRegistry - образец из методички (без изменений)
//   ЧАСТЬ 2. StudentRegistryPlus - доп. задание: своя функция getStudentsByMajor
//   ЧАСТЬ 3. BookRegistry, ExtendedBookRegistry - вариант 1 (книги)
//   ЧАСТЬ 4. EmployeeRegistry, ExtendedEmployeeRegistry - вариант 2 (сотрудники)
// В Remix: Compile, затем в списке CONTRACT выбрать нужный контракт и нажать Deploy.

// ################################################################
// ЧАСТЬ 1. ОБРАЗЕЦ ИЗ МЕТОДИЧКИ
// ################################################################

contract StudentRegistry {
    // ============================================================
    // DATA STRUCTURES
    // ============================================================
    
    struct Student {
        string name;
        uint256 age;
        string major;
        bool isActive;
    }
    
    // ============================================================
    // STATE VARIABLES
    // ============================================================
    
    address public admin;
    string public universityName;
    uint256 public studentCount;
    
    mapping(uint256 => Student) public students;
    mapping(address => uint256) public studentIdByAddress;
    
    // ============================================================
    // EVENTS
    // ============================================================
    
    event StudentRegistered(uint256 indexed studentId, string name);
    event StudentUpdated(uint256 indexed studentId, string name);
    event StudentDeleted(uint256 indexed studentId);
    
    // ============================================================
    // MODIFIERS
    // ============================================================
    
    modifier onlyAdmin() {
        require(msg.sender == admin, "Only admin can call this");
        _;
    }
    
    modifier studentExists(uint256 _studentId) {
        require(students[_studentId].isActive, "Student does not exist");
        _;
    }
    
    // ============================================================
    // CONSTRUCTOR
    // ============================================================
    
    constructor(string memory _universityName) {
        admin = msg.sender;
        universityName = _universityName;
        studentCount = 0;
    }
    
    // ============================================================
    // CORE FUNCTIONS
    // ============================================================
    
    function registerStudent(
        string memory _name,
        uint256 _age,
        string memory _major
    ) public onlyAdmin {
        require(bytes(_name).length > 0, "Name cannot be empty");
        require(_age > 0 && _age < 150, "Invalid age");
        require(bytes(_major).length > 0, "Major cannot be empty");
        
        studentCount++;
        
        students[studentCount] = Student({
            name: _name,
            age: _age,
            major: _major,
            isActive: true
        });
        
        studentIdByAddress[msg.sender] = studentCount;
        
        emit StudentRegistered(studentCount, _name);
    }
    
    function updateStudent(
        uint256 _studentId,
        string memory _newName,
        uint256 _newAge,
        string memory _newMajor
    ) public onlyAdmin studentExists(_studentId) {
        require(bytes(_newName).length > 0, "Name cannot be empty");
        require(_newAge > 0 && _newAge < 150, "Invalid age");
        require(bytes(_newMajor).length > 0, "Major cannot be empty");
        
        students[_studentId].name = _newName;
        students[_studentId].age = _newAge;
        students[_studentId].major = _newMajor;
        
        emit StudentUpdated(_studentId, _newName);
    }
    
    function deleteStudent(uint256 _studentId) public onlyAdmin studentExists(_studentId) {
        students[_studentId].isActive = false;
        emit StudentDeleted(_studentId);
    }
    
    // ============================================================
    // VIEW FUNCTIONS (READ ONLY)
    // ============================================================
    
    function getStudent(uint256 _studentId) 
        public 
        view 
        studentExists(_studentId) 
        returns (string memory, uint256, string memory, bool) 
    {
        Student memory s = students[_studentId];
        return (s.name, s.age, s.major, s.isActive);
    }
    
    function getStudentByAddress(address _address) 
        public 
        view 
        returns (uint256, string memory, uint256, string memory, bool) 
    {
        uint256 id = studentIdByAddress[_address];
        require(id > 0 && students[id].isActive, "Student not found");
        
        Student memory s = students[id];
        return (id, s.name, s.age, s.major, s.isActive);
    }
    
    function getAllStudents() 
        public 
        view 
        returns (uint256[] memory, string[] memory, uint256[] memory, string[] memory) 
    {
        uint256 activeCount = 0;
        
        // First count active students
        for (uint256 i = 1; i <= studentCount; i++) {
            if (students[i].isActive) {
                activeCount++;
            }
        }
        
        // Then fill arrays
        uint256[] memory ids = new uint256[](activeCount);
        string[] memory names = new string[](activeCount);
        uint256[] memory ages = new uint256[](activeCount);
        string[] memory majors = new string[](activeCount);
        
        uint256 index = 0;
        for (uint256 i = 1; i <= studentCount; i++) {
            if (students[i].isActive) {
                ids[index] = i;
                names[index] = students[i].name;
                ages[index] = students[i].age;
                majors[index] = students[i].major;
                index++;
            }
        }
        
        return (ids, names, ages, majors);
    }
    
    function getStudentCount() public view returns (uint256) {
        uint256 count = 0;
        for (uint256 i = 1; i <= studentCount; i++) {
            if (students[i].isActive) {
                count++;
            }
        }
        return count;
    }
    
    function isStudentRegistered(address _address) public view returns (bool) {
        uint256 id = studentIdByAddress[_address];
        return id > 0 && students[id].isActive;
    }
    
    // ============================================================
    // ADMIN FUNCTIONS
    // ============================================================
    
    function changeAdmin(address _newAdmin) public onlyAdmin {
        require(_newAdmin != address(0), "Invalid address");
        admin = _newAdmin;
    }
    
    function setUniversityName(string memory _newName) public onlyAdmin {
        require(bytes(_newName).length > 0, "Name cannot be empty");
        universityName = _newName;
    }
}

// ============================================================
// EXTRA: Simple contract to demonstrate inheritance
// ============================================================

contract ExtendedStudentRegistry is StudentRegistry {
    string public department;
    
    constructor(string memory _universityName, string memory _department) 
        StudentRegistry(_universityName) 
    {
        department = _department;
    }
    
    function setDepartment(string memory _newDepartment) public onlyAdmin {
        department = _newDepartment;
    }
    
    function getStudentWithMajor(uint256 _studentId, string memory _major) 
        public 
        view 
        studentExists(_studentId) 
        returns (bool) 
    {
        return keccak256(bytes(students[_studentId].major)) == keccak256(bytes(_major));
    }
}


// ################################################################
// ЧАСТЬ 2. ДОП. ЗАДАНИЕ: СВОЯ ФУНКЦИЯ getStudentsByMajor
// ################################################################

// ============================================================
// ДОПОЛНИТЕЛЬНОЕ ЗАДАНИЕ: своя функция getStudentsByMajor
// Контракты выше взяты из методички без изменений.
// ============================================================

contract StudentRegistryPlus is ExtendedStudentRegistry {

    constructor(string memory _universityName, string memory _department)
        ExtendedStudentRegistry(_universityName, _department)
    {}

    /// @notice Возвращает номера (id) всех действующих студентов заданной специальности.
    /// @dev view-функция: только читает данные, газ при вызове снаружи не тратится.
    function getStudentsByMajor(string memory _major)
        public
        view
        returns (uint256[] memory)
    {
        // Строки в Solidity нельзя сравнивать через ==, поэтому сравниваем их хэши.
        bytes32 wanted = keccak256(bytes(_major));

        // Шаг 1. Считаем, сколько студентов подходит (размер массива в памяти задается заранее).
        uint256 matches = 0;
        for (uint256 i = 1; i <= studentCount; i++) {
            if (students[i].isActive && keccak256(bytes(students[i].major)) == wanted) {
                matches++;
            }
        }

        // Шаг 2. Создаем массив нужной длины и заполняем его номерами.
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
}


// ################################################################
// ЧАСТЬ 3. ВАРИАНТ 1 - СПРАВОЧНИК КНИГ
// ################################################################

/// @title BookRegistry - справочник книг (Лабораторная работа 7, вариант 1)
/// @notice Хранит: название, автор, год издания, наличие, жанр.
contract BookRegistry {
    // ============================================================
    // DATA STRUCTURES
    // ============================================================

    struct Book {
        string title;       // название
        string author;      // автор
        uint256 year;       // год издания
        bool available;     // наличие (есть в библиотеке / выдана)
        string genre;       // дополнительное поле: жанр
        bool exists;        // false после удаления (мягкое удаление)
    }

    // ============================================================
    // STATE VARIABLES
    // ============================================================

    address public admin;
    string public libraryName;
    uint256 public bookCount; // последний выданный id (удаленные тоже учитываются)

    mapping(uint256 => Book) public books;

    // ============================================================
    // EVENTS
    // ============================================================

    event BookAdded(uint256 indexed bookId, string title, string author);
    event BookUpdated(uint256 indexed bookId, string title);
    event BookDeleted(uint256 indexed bookId);
    event AvailabilityChanged(uint256 indexed bookId, bool available);
    event AdminChanged(address indexed oldAdmin, address indexed newAdmin);

    // ============================================================
    // MODIFIERS
    // ============================================================

    modifier onlyAdmin() {
        require(msg.sender == admin, "Only admin can call this");
        _;
    }

    modifier bookExists(uint256 _bookId) {
        require(books[_bookId].exists, "Book does not exist");
        _;
    }

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    constructor(string memory _libraryName) {
        require(bytes(_libraryName).length > 0, "Name cannot be empty");
        admin = msg.sender;
        libraryName = _libraryName;
    }

    // ============================================================
    // INTERNAL VALIDATION
    // ============================================================

    function _validate(string memory _title, string memory _author, uint256 _year) internal pure {
        require(bytes(_title).length > 0, "Title cannot be empty");
        require(bytes(_author).length > 0, "Author cannot be empty");
        require(_year > 0 && _year <= 2100, "Invalid year");
    }

    // ============================================================
    // CORE FUNCTIONS (CRUD)
    // ============================================================

    function addBook(
        string memory _title,
        string memory _author,
        uint256 _year,
        bool _available,
        string memory _genre
    ) public onlyAdmin returns (uint256) {
        _validate(_title, _author, _year);

        bookCount++;
        books[bookCount] = Book({
            title: _title,
            author: _author,
            year: _year,
            available: _available,
            genre: _genre,
            exists: true
        });

        emit BookAdded(bookCount, _title, _author);
        return bookCount;
    }

    function updateBook(
        uint256 _bookId,
        string memory _title,
        string memory _author,
        uint256 _year,
        string memory _genre
    ) public onlyAdmin bookExists(_bookId) {
        _validate(_title, _author, _year);

        Book storage b = books[_bookId];
        b.title = _title;
        b.author = _author;
        b.year = _year;
        b.genre = _genre;

        emit BookUpdated(_bookId, _title);
    }

    function setAvailability(uint256 _bookId, bool _available)
        public
        onlyAdmin
        bookExists(_bookId)
    {
        books[_bookId].available = _available;
        emit AvailabilityChanged(_bookId, _available);
    }

    function deleteBook(uint256 _bookId) public onlyAdmin bookExists(_bookId) {
        books[_bookId].exists = false;
        emit BookDeleted(_bookId);
    }

    // ============================================================
    // VIEW FUNCTIONS (READ ONLY)
    // ============================================================

    function getBook(uint256 _bookId)
        public
        view
        bookExists(_bookId)
        returns (
            string memory title,
            string memory author,
            uint256 year,
            bool available,
            string memory genre
        )
    {
        Book storage b = books[_bookId];
        return (b.title, b.author, b.year, b.available, b.genre);
    }

    /// @notice Все существующие книги (массивы одинаковой длины).
    function getAllBooks()
        public
        view
        returns (
            uint256[] memory ids,
            string[] memory titles,
            string[] memory authors,
            uint256[] memory yearList,
            bool[] memory availability
        )
    {
        uint256 n = getBookCount();
        ids = new uint256[](n);
        titles = new string[](n);
        authors = new string[](n);
        yearList = new uint256[](n);
        availability = new bool[](n);

        uint256 k = 0;
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].exists) {
                ids[k] = i;
                titles[k] = books[i].title;
                authors[k] = books[i].author;
                yearList[k] = books[i].year;
                availability[k] = books[i].available;
                k++;
            }
        }
    }

    /// @notice Число существующих (не удаленных) книг.
    function getBookCount() public view returns (uint256 count) {
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].exists) count++;
        }
    }

    /// @notice Id всех книг, которые сейчас есть в наличии.
    function getAvailableBookIds() public view returns (uint256[] memory) {
        uint256 n = 0;
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].exists && books[i].available) n++;
        }
        uint256[] memory result = new uint256[](n);
        uint256 k = 0;
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].exists && books[i].available) result[k++] = i;
        }
        return result;
    }

    /// @notice Id всех книг заданного автора (точное совпадение строки).
    function findByAuthor(string memory _author) public view returns (uint256[] memory) {
        bytes32 h = keccak256(bytes(_author));
        uint256 n = 0;
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].exists && keccak256(bytes(books[i].author)) == h) n++;
        }
        uint256[] memory result = new uint256[](n);
        uint256 k = 0;
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].exists && keccak256(bytes(books[i].author)) == h) result[k++] = i;
        }
        return result;
    }

    // ============================================================
    // ADMIN FUNCTIONS
    // ============================================================

    function changeAdmin(address _newAdmin) public onlyAdmin {
        require(_newAdmin != address(0), "Invalid address");
        emit AdminChanged(admin, _newAdmin);
        admin = _newAdmin;
    }

    function setLibraryName(string memory _newName) public onlyAdmin {
        require(bytes(_newName).length > 0, "Name cannot be empty");
        libraryName = _newName;
    }
}

// ============================================================
// EXTRA: наследование (как ExtendedStudentRegistry в методичке)
// ============================================================

contract ExtendedBookRegistry is BookRegistry {
    string public branch; // филиал библиотеки

    constructor(string memory _libraryName, string memory _branch)
        BookRegistry(_libraryName)
    {
        branch = _branch;
    }

    function setBranch(string memory _newBranch) public onlyAdmin {
        branch = _newBranch;
    }

    /// @notice Проверка: относится ли книга к указанному жанру.
    function isGenre(uint256 _bookId, string memory _genre)
        public
        view
        bookExists(_bookId)
        returns (bool)
    {
        return keccak256(bytes(books[_bookId].genre)) == keccak256(bytes(_genre));
    }
}


// ################################################################
// ЧАСТЬ 4. ВАРИАНТ 2 - СПРАВОЧНИК СОТРУДНИКОВ
// ################################################################

/// @title EmployeeRegistry - справочник сотрудников фирмы (Лабораторная работа 7, вариант 2)
/// @notice Хранит: фамилия, имя, год рождения, должность, стаж (лет), оклад.
/// @dev Оклад - обычное число (условные денежные единицы), переводов средств нет.
contract EmployeeRegistry {
    // ============================================================
    // DATA STRUCTURES
    // ============================================================

    struct Employee {
        string lastName;     // фамилия
        string firstName;    // имя
        uint256 birthYear;   // год рождения
        string position;     // должность
        uint256 experience;  // стаж, полных лет
        uint256 salary;      // оклад
        bool isActive;       // false после увольнения (мягкое удаление)
    }

    // ============================================================
    // STATE VARIABLES
    // ============================================================

    address public admin;
    string public companyName;
    uint256 public employeeCount; // последний выданный id

    mapping(uint256 => Employee) public employees;

    // ============================================================
    // EVENTS
    // ============================================================

    event EmployeeAdded(uint256 indexed employeeId, string lastName, string firstName);
    event EmployeeUpdated(uint256 indexed employeeId, string lastName);
    event EmployeeFired(uint256 indexed employeeId);
    event SalaryChanged(uint256 indexed employeeId, uint256 oldSalary, uint256 newSalary);
    event AdminChanged(address indexed oldAdmin, address indexed newAdmin);

    // ============================================================
    // MODIFIERS
    // ============================================================

    modifier onlyAdmin() {
        require(msg.sender == admin, "Only admin can call this");
        _;
    }

    modifier employeeExists(uint256 _id) {
        require(employees[_id].isActive, "Employee does not exist");
        _;
    }

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    constructor(string memory _companyName) {
        require(bytes(_companyName).length > 0, "Name cannot be empty");
        admin = msg.sender;
        companyName = _companyName;
    }

    // ============================================================
    // INTERNAL VALIDATION
    // ============================================================

    function _validate(
        string memory _lastName,
        string memory _firstName,
        uint256 _birthYear,
        string memory _position,
        uint256 _experience,
        uint256 _salary
    ) internal pure {
        require(bytes(_lastName).length > 0, "Last name cannot be empty");
        require(bytes(_firstName).length > 0, "First name cannot be empty");
        require(bytes(_position).length > 0, "Position cannot be empty");
        require(_birthYear >= 1900 && _birthYear <= 2100, "Invalid birth year");
        require(_experience <= 80, "Invalid experience");
        require(_salary > 0, "Salary must be positive");
    }

    // ============================================================
    // CORE FUNCTIONS (CRUD)
    // ============================================================

    function addEmployee(
        string memory _lastName,
        string memory _firstName,
        uint256 _birthYear,
        string memory _position,
        uint256 _experience,
        uint256 _salary
    ) public onlyAdmin returns (uint256) {
        _validate(_lastName, _firstName, _birthYear, _position, _experience, _salary);

        employeeCount++;
        employees[employeeCount] = Employee({
            lastName: _lastName,
            firstName: _firstName,
            birthYear: _birthYear,
            position: _position,
            experience: _experience,
            salary: _salary,
            isActive: true
        });

        emit EmployeeAdded(employeeCount, _lastName, _firstName);
        return employeeCount;
    }

    function updateEmployee(
        uint256 _id,
        string memory _lastName,
        string memory _firstName,
        uint256 _birthYear,
        string memory _position,
        uint256 _experience,
        uint256 _salary
    ) public onlyAdmin employeeExists(_id) {
        _validate(_lastName, _firstName, _birthYear, _position, _experience, _salary);

        Employee storage e = employees[_id];
        uint256 oldSalary = e.salary;
        e.lastName = _lastName;
        e.firstName = _firstName;
        e.birthYear = _birthYear;
        e.position = _position;
        e.experience = _experience;
        e.salary = _salary;

        emit EmployeeUpdated(_id, _lastName);
        if (oldSalary != _salary) emit SalaryChanged(_id, oldSalary, _salary);
    }

    function setSalary(uint256 _id, uint256 _newSalary) public onlyAdmin employeeExists(_id) {
        require(_newSalary > 0, "Salary must be positive");
        uint256 old = employees[_id].salary;
        employees[_id].salary = _newSalary;
        emit SalaryChanged(_id, old, _newSalary);
    }

    /// @notice Повысить оклад на заданный процент (целое число процентов, 1..100).
    function raiseSalary(uint256 _id, uint256 _percent) public onlyAdmin employeeExists(_id) {
        require(_percent > 0 && _percent <= 100, "Percent must be 1..100");
        uint256 old = employees[_id].salary;
        uint256 updated = old + (old * _percent) / 100;
        employees[_id].salary = updated;
        emit SalaryChanged(_id, old, updated);
    }

    function setPosition(uint256 _id, string memory _position)
        public
        onlyAdmin
        employeeExists(_id)
    {
        require(bytes(_position).length > 0, "Position cannot be empty");
        employees[_id].position = _position;
        emit EmployeeUpdated(_id, employees[_id].lastName);
    }

    function fireEmployee(uint256 _id) public onlyAdmin employeeExists(_id) {
        employees[_id].isActive = false;
        emit EmployeeFired(_id);
    }

    // ============================================================
    // VIEW FUNCTIONS (READ ONLY)
    // ============================================================

    function getEmployee(uint256 _id)
        public
        view
        employeeExists(_id)
        returns (
            string memory lastName,
            string memory firstName,
            uint256 birthYear,
            string memory position,
            uint256 experience,
            uint256 salary
        )
    {
        Employee storage e = employees[_id];
        return (e.lastName, e.firstName, e.birthYear, e.position, e.experience, e.salary);
    }

    function getAllEmployees()
        public
        view
        returns (
            uint256[] memory ids,
            string[] memory lastNames,
            string[] memory firstNames,
            string[] memory positions,
            uint256[] memory salaries
        )
    {
        uint256 n = getEmployeeCount();
        ids = new uint256[](n);
        lastNames = new string[](n);
        firstNames = new string[](n);
        positions = new string[](n);
        salaries = new uint256[](n);

        uint256 k = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive) {
                ids[k] = i;
                lastNames[k] = employees[i].lastName;
                firstNames[k] = employees[i].firstName;
                positions[k] = employees[i].position;
                salaries[k] = employees[i].salary;
                k++;
            }
        }
    }

    /// @notice Число работающих сотрудников.
    function getEmployeeCount() public view returns (uint256 count) {
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive) count++;
        }
    }

    /// @notice Суммарный фонд оплаты труда (сумма окладов работающих).
    function totalPayroll() public view returns (uint256 total) {
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive) total += employees[i].salary;
        }
    }

    /// @notice Средний оклад (целочисленное деление; 0, если сотрудников нет).
    function averageSalary() public view returns (uint256) {
        uint256 n = getEmployeeCount();
        if (n == 0) return 0;
        return totalPayroll() / n;
    }

    /// @notice Id сотрудников на заданной должности (точное совпадение строки).
    function findByPosition(string memory _position) public view returns (uint256[] memory) {
        bytes32 h = keccak256(bytes(_position));
        uint256 n = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive && keccak256(bytes(employees[i].position)) == h) n++;
        }
        uint256[] memory result = new uint256[](n);
        uint256 k = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive && keccak256(bytes(employees[i].position)) == h) {
                result[k++] = i;
            }
        }
        return result;
    }

    /// @notice Id сотрудников со стажем не меньше заданного.
    function findByMinExperience(uint256 _minYears) public view returns (uint256[] memory) {
        uint256 n = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive && employees[i].experience >= _minYears) n++;
        }
        uint256[] memory result = new uint256[](n);
        uint256 k = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive && employees[i].experience >= _minYears) result[k++] = i;
        }
        return result;
    }

    // ============================================================
    // ADMIN FUNCTIONS
    // ============================================================

    function changeAdmin(address _newAdmin) public onlyAdmin {
        require(_newAdmin != address(0), "Invalid address");
        emit AdminChanged(admin, _newAdmin);
        admin = _newAdmin;
    }

    function setCompanyName(string memory _newName) public onlyAdmin {
        require(bytes(_newName).length > 0, "Name cannot be empty");
        companyName = _newName;
    }
}

// ============================================================
// EXTRA: наследование (как ExtendedStudentRegistry в методичке)
// ============================================================

contract ExtendedEmployeeRegistry is EmployeeRegistry {
    string public department; // отдел

    constructor(string memory _companyName, string memory _department)
        EmployeeRegistry(_companyName)
    {
        department = _department;
    }

    function setDepartment(string memory _newDepartment) public onlyAdmin {
        department = _newDepartment;
    }

    /// @notice Проверка: занимает ли сотрудник указанную должность.
    function hasPosition(uint256 _id, string memory _position)
        public
        view
        employeeExists(_id)
        returns (bool)
    {
        return keccak256(bytes(employees[_id].position)) == keccak256(bytes(_position));
    }
}
