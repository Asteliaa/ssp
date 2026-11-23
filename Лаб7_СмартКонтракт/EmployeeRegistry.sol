// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;
contract EmployeeRegistry {
    // ============================================================
    // DATA STRUCTURES
    // ============================================================
    struct Employee {
        string lastName;
        string firstName;
        uint256 birthYear;
        string position;
        uint256 experience;
        uint256 salary;
        bool isActive;
    }
    // ============================================================
    // STATE VARIABLES
    // ============================================================
    address public admin;
    string public companyName;
    uint256 public employeeCount;
    mapping(uint256 => Employee) public employees;
    // ============================================================
    // EVENTS
    // ============================================================
    event EmployeeAdded(uint256 indexed employeeId, string lastName);
    event EmployeeUpdated(uint256 indexed employeeId, string lastName);
    event EmployeeFired(uint256 indexed employeeId);
    // ============================================================
    // MODIFIERS
    // ============================================================
    modifier onlyAdmin() {
        require(msg.sender == admin, "Only admin can call this");
        _;
    }
    modifier employeeExists(uint256 _employeeId) {
        require(employees[_employeeId].isActive, "Employee does not exist");
        _;
    }
    // ============================================================
    // CONSTRUCTOR
    // ============================================================
    constructor(string memory _companyName) {
        admin = msg.sender;
        companyName = _companyName;
        employeeCount = 0;
    }
    // ============================================================
    // CORE FUNCTIONS
    // ============================================================
    function addEmployee(
        string memory _lastName,
        string memory _firstName,
        uint256 _birthYear,
        string memory _position,
        uint256 _experience,
        uint256 _salary
    ) public onlyAdmin {
        require(bytes(_lastName).length > 0, "Last name cannot be empty");
        require(bytes(_firstName).length > 0, "First name cannot be empty");
        require(_birthYear >= 1900 && _birthYear <= 2100, "Invalid birth year");
        require(bytes(_position).length > 0, "Position cannot be empty");
        require(_experience <= 80, "Invalid experience");
        require(_salary > 0, "Salary must be positive");
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
        emit EmployeeAdded(employeeCount, _lastName);
    }
    function updateEmployee(
        uint256 _employeeId,
        string memory _newLastName,
        string memory _newFirstName,
        uint256 _newBirthYear,
        string memory _newPosition,
        uint256 _newExperience,
        uint256 _newSalary
    ) public onlyAdmin employeeExists(_employeeId) {
        require(bytes(_newLastName).length > 0, "Last name cannot be empty");
        require(bytes(_newFirstName).length > 0, "First name cannot be empty");
        require(_newBirthYear >= 1900 && _newBirthYear <= 2100, "Invalid birth year");
        require(bytes(_newPosition).length > 0, "Position cannot be empty");
        require(_newExperience <= 80, "Invalid experience");
        require(_newSalary > 0, "Salary must be positive");
        employees[_employeeId].lastName = _newLastName;
        employees[_employeeId].firstName = _newFirstName;
        employees[_employeeId].birthYear = _newBirthYear;
        employees[_employeeId].position = _newPosition;
        employees[_employeeId].experience = _newExperience;
        employees[_employeeId].salary = _newSalary;
        emit EmployeeUpdated(_employeeId, _newLastName);
    }
    function setSalary(uint256 _employeeId, uint256 _newSalary) public onlyAdmin employeeExists(_employeeId) {
        require(_newSalary > 0, "Salary must be positive");
        employees[_employeeId].salary = _newSalary;
        emit EmployeeUpdated(_employeeId, employees[_employeeId].lastName);
    }
    function raiseSalary(uint256 _employeeId, uint256 _percent) public onlyAdmin employeeExists(_employeeId) {
        require(_percent > 0 && _percent <= 100, "Percent must be 1..100");
        employees[_employeeId].salary = employees[_employeeId].salary + employees[_employeeId].salary * _percent / 100;
        emit EmployeeUpdated(_employeeId, employees[_employeeId].lastName);
    }
    function setPosition(uint256 _employeeId, string memory _newPosition) public onlyAdmin employeeExists(_employeeId) {
        require(bytes(_newPosition).length > 0, "Position cannot be empty");
        employees[_employeeId].position = _newPosition;
        emit EmployeeUpdated(_employeeId, employees[_employeeId].lastName);
    }
    function fireEmployee(uint256 _employeeId) public onlyAdmin employeeExists(_employeeId) {
        employees[_employeeId].isActive = false;
        emit EmployeeFired(_employeeId);
    }
    // ============================================================
    // VIEW FUNCTIONS (READ ONLY)
    // ============================================================
    function getEmployee(uint256 _employeeId)
        public
        view
        employeeExists(_employeeId)
        returns (string memory, string memory, uint256, string memory, uint256, uint256)
    {
        Employee memory e = employees[_employeeId];
        return (e.lastName, e.firstName, e.birthYear, e.position, e.experience, e.salary);
    }
    function getAllEmployees()
        public
        view
        returns (uint256[] memory, string[] memory, string[] memory, string[] memory, uint256[] memory)
    {
        uint256 activeCount = 0;
        // First count active employees
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive) {
                activeCount++;
            }
        }
        // Then fill arrays
        uint256[] memory ids = new uint256[](activeCount);
        string[] memory lastNames = new string[](activeCount);
        string[] memory firstNames = new string[](activeCount);
        string[] memory positions = new string[](activeCount);
        uint256[] memory salaries = new uint256[](activeCount);
        uint256 index = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive) {
                ids[index] = i;
                lastNames[index] = employees[i].lastName;
                firstNames[index] = employees[i].firstName;
                positions[index] = employees[i].position;
                salaries[index] = employees[i].salary;
                index++;
            }
        }
        return (ids, lastNames, firstNames, positions, salaries);
    }
    function getEmployeeCount() public view returns (uint256) {
        uint256 count = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive) {
                count++;
            }
        }
        return count;
    }
    function totalPayroll() public view returns (uint256) {
        uint256 total = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive) {
                total += employees[i].salary;
            }
        }
        return total;
    }
    function averageSalary() public view returns (uint256) {
        uint256 count = getEmployeeCount();
        if (count == 0) {
            return 0;
        }
        return totalPayroll() / count;
    }
    function findByPosition(string memory _position) public view returns (uint256[] memory) {
        uint256 positionCount = 0;
        // First count employees with this position
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive && keccak256(bytes(employees[i].position)) == keccak256(bytes(_position))) {
                positionCount++;
            }
        }
        // Then fill array
        uint256[] memory ids = new uint256[](positionCount);
        uint256 index = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive && keccak256(bytes(employees[i].position)) == keccak256(bytes(_position))) {
                ids[index] = i;
                index++;
            }
        }
        return ids;
    }
    function findByMinExperience(uint256 _minYears) public view returns (uint256[] memory) {
        uint256 expCount = 0;
        // First count employees with enough experience
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive && employees[i].experience >= _minYears) {
                expCount++;
            }
        }
        // Then fill array
        uint256[] memory ids = new uint256[](expCount);
        uint256 index = 0;
        for (uint256 i = 1; i <= employeeCount; i++) {
            if (employees[i].isActive && employees[i].experience >= _minYears) {
                ids[index] = i;
                index++;
            }
        }
        return ids;
    }
    // ============================================================
    // ADMIN FUNCTIONS
    // ============================================================
    function changeAdmin(address _newAdmin) public onlyAdmin {
        require(_newAdmin != address(0), "Invalid address");
        admin = _newAdmin;
    }
    function setCompanyName(string memory _newName) public onlyAdmin {
        require(bytes(_newName).length > 0, "Name cannot be empty");
        companyName = _newName;
    }
}
// ============================================================
// EXTRA: Simple contract to demonstrate inheritance
// ============================================================
contract ExtendedEmployeeRegistry is EmployeeRegistry {
    string public department;
    constructor(string memory _companyName, string memory _department)
        EmployeeRegistry(_companyName)
    {
        department = _department;
    }
    function setDepartment(string memory _newDepartment) public onlyAdmin {
        department = _newDepartment;
    }
    function getEmployeeWithPosition(uint256 _employeeId, string memory _position)
        public
        view
        employeeExists(_employeeId)
        returns (bool)
    {
        return keccak256(bytes(employees[_employeeId].position)) == keccak256(bytes(_position));
    }
}
