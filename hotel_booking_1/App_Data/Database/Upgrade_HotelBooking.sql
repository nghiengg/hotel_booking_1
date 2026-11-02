IF OBJECT_ID('dbo.ProductCategory','U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductCategory(
        CategoryID INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName NVARCHAR(150) NOT NULL UNIQUE
    );
END;
IF OBJECT_ID('dbo.Product','U') IS NULL
BEGIN
    CREATE TABLE dbo.Product(
        ProductID INT IDENTITY(1,1) PRIMARY KEY,
        CategoryID INT NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Price DECIMAL(18,2) NOT NULL CHECK(Price>=0),
        ImageUrl NVARCHAR(1000) NULL,
        StockQuantity INT NOT NULL DEFAULT(0) CHECK(StockQuantity>=0),
        IsActive BIT NOT NULL DEFAULT(1),
        CONSTRAINT FK_Product_ProductCategory FOREIGN KEY(CategoryID) REFERENCES dbo.ProductCategory(CategoryID)
    );
END;
IF OBJECT_ID('dbo.Orders','U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders(
        OrderID INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(255) NOT NULL,
        ReceiverName NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(30) NOT NULL,
        ShippingAddress NVARCHAR(500) NOT NULL,
        OrderDate DATETIME NOT NULL DEFAULT(GETDATE()),
        TotalAmount DECIMAL(18,2) NOT NULL,
        OrderStatus NVARCHAR(30) NOT NULL DEFAULT(N'Chờ duyệt')
    );
END;
IF OBJECT_ID('dbo.OrderItem','U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItem(
        OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL,
        ProductID INT NOT NULL,
        Quantity INT NOT NULL CHECK(Quantity>0),
        UnitPrice DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_OrderItem_Orders FOREIGN KEY(OrderID) REFERENCES dbo.Orders(OrderID),
        CONSTRAINT FK_OrderItem_Product FOREIGN KEY(ProductID) REFERENCES dbo.Product(ProductID)
    );
END;
IF OBJECT_ID('dbo.Hotel','U') IS NULL
BEGIN
    CREATE TABLE dbo.Hotel(
        HotelID INT IDENTITY(1,1) PRIMARY KEY,
        HotelName NVARCHAR(200) NOT NULL,
        City NVARCHAR(120) NULL,
        Address NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        ImageUrl NVARCHAR(1000) NULL
    );
END;
IF COL_LENGTH('dbo.Room','HotelID') IS NULL ALTER TABLE dbo.Room ADD HotelID INT NULL;
IF COL_LENGTH('dbo.Room','MaxGuests') IS NULL ALTER TABLE dbo.Room ADD MaxGuests INT NOT NULL CONSTRAINT DF_Room_MaxGuests DEFAULT(2);
IF COL_LENGTH('dbo.Room','TotalQuantity') IS NULL ALTER TABLE dbo.Room ADD TotalQuantity INT NOT NULL CONSTRAINT DF_Room_TotalQuantity DEFAULT(1);
IF OBJECT_ID('dbo.Amenity','U') IS NULL
BEGIN
    CREATE TABLE dbo.Amenity(AmenityID INT IDENTITY(1,1) PRIMARY KEY, AmenityName NVARCHAR(120) NOT NULL UNIQUE);
END;
IF OBJECT_ID('dbo.RoomAmenity','U') IS NULL
BEGIN
    CREATE TABLE dbo.RoomAmenity(
        RoomID INT NOT NULL, AmenityID INT NOT NULL,
        CONSTRAINT PK_RoomAmenity PRIMARY KEY(RoomID,AmenityID),
        CONSTRAINT FK_RoomAmenity_Room FOREIGN KEY(RoomID) REFERENCES dbo.Room(RoomID) ON DELETE CASCADE,
        CONSTRAINT FK_RoomAmenity_Amenity FOREIGN KEY(AmenityID) REFERENCES dbo.Amenity(AmenityID) ON DELETE CASCADE
    );
END;
IF NOT EXISTS(SELECT 1 FROM dbo.ProductCategory)
    INSERT dbo.ProductCategory(CategoryName) VALUES(N'Đồ gia dụng'),(N'Đồ dùng du lịch'),(N'Phụ kiện');
IF NOT EXISTS(SELECT 1 FROM dbo.Amenity)
    INSERT dbo.Amenity(AmenityName) VALUES(N'Wi-Fi miễn phí'),(N'Điều hòa'),(N'Tivi'),(N'Bữa sáng'),(N'Hồ bơi'),(N'Chỗ đỗ xe');

/* ===== Khách sạn nổi bật + Giỏ hàng khách sạn (yêu thích / đặt sau) ===== */
IF COL_LENGTH('dbo.Hotel','IsFeatured') IS NULL ALTER TABLE dbo.Hotel ADD IsFeatured BIT NOT NULL CONSTRAINT DF_Hotel_IsFeatured DEFAULT(0);
IF OBJECT_ID('dbo.HotelCart','U') IS NULL
BEGIN
    CREATE TABLE dbo.HotelCart(
        CartID INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(255) NOT NULL,
        HotelID INT NOT NULL,
        RoomID INT NULL,
        CheckIn DATETIME NULL,
        CheckOut DATETIME NULL,
        Quantity INT NOT NULL DEFAULT(1),
        CreatedAt DATETIME NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT FK_HotelCart_Hotel FOREIGN KEY(HotelID) REFERENCES dbo.Hotel(HotelID) ON DELETE CASCADE
    );
END;

/* Security: passwords are hashed by the application. Existing accounts remain usable. */
PRINT N'HotelBooking schema upgrade completed.';

/* Promote an existing account to administrator when needed (replace the email):
UPDATE dbo.[User] SET UserRole='A' WHERE Username='admin@example.com';
*/
