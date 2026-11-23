// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;
contract BookRegistry {
    // ============================================================
    // DATA STRUCTURES
    // ============================================================
    struct Book {
        string title;
        string author;
        uint256 year;
        bool available;
        string genre;
        bool isActive;
    }
    // ============================================================
    // STATE VARIABLES
    // ============================================================
    address public admin;
    string public libraryName;
    uint256 public bookCount;
    mapping(uint256 => Book) public books;
    // ============================================================
    // EVENTS
    // ============================================================
    event BookAdded(uint256 indexed bookId, string title);
    event BookUpdated(uint256 indexed bookId, string title);
    event BookDeleted(uint256 indexed bookId);
    // ============================================================
    // MODIFIERS
    // ============================================================
    modifier onlyAdmin() {
        require(msg.sender == admin, "Only admin can call this");
        _;
    }
    modifier bookExists(uint256 _bookId) {
        require(books[_bookId].isActive, "Book does not exist");
        _;
    }
    // ============================================================
    // CONSTRUCTOR
    // ============================================================
    constructor(string memory _libraryName) {
        admin = msg.sender;
        libraryName = _libraryName;
        bookCount = 0;
    }
    // ============================================================
    // CORE FUNCTIONS
    // ============================================================
    function addBook(
        string memory _title,
        string memory _author,
        uint256 _year,
        bool _available,
        string memory _genre
    ) public onlyAdmin {
        require(bytes(_title).length > 0, "Title cannot be empty");
        require(bytes(_author).length > 0, "Author cannot be empty");
        require(_year > 0 && _year <= 2100, "Invalid year");
        bookCount++;
        books[bookCount] = Book({
            title: _title,
            author: _author,
            year: _year,
            available: _available,
            genre: _genre,
            isActive: true
        });
        emit BookAdded(bookCount, _title);
    }
    function updateBook(
        uint256 _bookId,
        string memory _newTitle,
        string memory _newAuthor,
        uint256 _newYear,
        string memory _newGenre
    ) public onlyAdmin bookExists(_bookId) {
        require(bytes(_newTitle).length > 0, "Title cannot be empty");
        require(bytes(_newAuthor).length > 0, "Author cannot be empty");
        require(_newYear > 0 && _newYear <= 2100, "Invalid year");
        books[_bookId].title = _newTitle;
        books[_bookId].author = _newAuthor;
        books[_bookId].year = _newYear;
        books[_bookId].genre = _newGenre;
        emit BookUpdated(_bookId, _newTitle);
    }
    function setAvailability(uint256 _bookId, bool _available) public onlyAdmin bookExists(_bookId) {
        books[_bookId].available = _available;
        emit BookUpdated(_bookId, books[_bookId].title);
    }
    function deleteBook(uint256 _bookId) public onlyAdmin bookExists(_bookId) {
        books[_bookId].isActive = false;
        emit BookDeleted(_bookId);
    }
    // ============================================================
    // VIEW FUNCTIONS (READ ONLY)
    // ============================================================
    function getBook(uint256 _bookId)
        public
        view
        bookExists(_bookId)
        returns (string memory, string memory, uint256, bool, string memory)
    {
        Book memory b = books[_bookId];
        return (b.title, b.author, b.year, b.available, b.genre);
    }
    function getAllBooks()
        public
        view
        returns (uint256[] memory, string[] memory, string[] memory, uint256[] memory, bool[] memory)
    {
        uint256 activeCount = 0;
        // First count active books
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].isActive) {
                activeCount++;
            }
        }
        // Then fill arrays
        uint256[] memory ids = new uint256[](activeCount);
        string[] memory titles = new string[](activeCount);
        string[] memory authors = new string[](activeCount);
        uint256[] memory yearList = new uint256[](activeCount);
        bool[] memory availability = new bool[](activeCount);
        uint256 index = 0;
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].isActive) {
                ids[index] = i;
                titles[index] = books[i].title;
                authors[index] = books[i].author;
                yearList[index] = books[i].year;
                availability[index] = books[i].available;
                index++;
            }
        }
        return (ids, titles, authors, yearList, availability);
    }
    function getBookCount() public view returns (uint256) {
        uint256 count = 0;
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].isActive) {
                count++;
            }
        }
        return count;
    }
    function getAvailableBookIds() public view returns (uint256[] memory) {
        uint256 availableCount = 0;
        // First count available books
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].isActive && books[i].available) {
                availableCount++;
            }
        }
        // Then fill array
        uint256[] memory ids = new uint256[](availableCount);
        uint256 index = 0;
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].isActive && books[i].available) {
                ids[index] = i;
                index++;
            }
        }
        return ids;
    }
    function findByAuthor(string memory _author) public view returns (uint256[] memory) {
        uint256 authorCount = 0;
        // First count books of this author
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].isActive && keccak256(bytes(books[i].author)) == keccak256(bytes(_author))) {
                authorCount++;
            }
        }
        // Then fill array
        uint256[] memory ids = new uint256[](authorCount);
        uint256 index = 0;
        for (uint256 i = 1; i <= bookCount; i++) {
            if (books[i].isActive && keccak256(bytes(books[i].author)) == keccak256(bytes(_author))) {
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
    function setLibraryName(string memory _newName) public onlyAdmin {
        require(bytes(_newName).length > 0, "Name cannot be empty");
        libraryName = _newName;
    }
}
// ============================================================
// EXTRA: Simple contract to demonstrate inheritance
// ============================================================
contract ExtendedBookRegistry is BookRegistry {
    string public branch;
    constructor(string memory _libraryName, string memory _branch)
        BookRegistry(_libraryName)
    {
        branch = _branch;
    }
    function setBranch(string memory _newBranch) public onlyAdmin {
        branch = _newBranch;
    }
    function getBookWithGenre(uint256 _bookId, string memory _genre)
        public
        view
        bookExists(_bookId)
        returns (bool)
    {
        return keccak256(bytes(books[_bookId].genre)) == keccak256(bytes(_genre));
    }
}
