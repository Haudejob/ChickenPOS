IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
CREATE TABLE Roles(Id int IDENTITY PRIMARY KEY, Name nvarchar(30) NOT NULL UNIQUE);
CREATE TABLE Users(Id int IDENTITY PRIMARY KEY, RoleId int NOT NULL REFERENCES Roles(Id), Username nvarchar(60) NOT NULL UNIQUE, DisplayName nvarchar(100) NOT NULL, PasswordHash nvarchar(512) NOT NULL, IsActive bit NOT NULL DEFAULT 1);
CREATE TABLE Product(Id int IDENTITY PRIMARY KEY, Name nvarchar(120) NOT NULL, Price decimal(18,0) NOT NULL CHECK(Price>=0), Category nvarchar(40) NOT NULL, Icon nvarchar(10) NOT NULL, IsActive bit NOT NULL DEFAULT 1, Version rowversion);
CREATE TABLE DiningTable(Id int IDENTITY PRIMARY KEY, Name nvarchar(40) NOT NULL UNIQUE);
CREATE TABLE DiningSession(Id int IDENTITY PRIMARY KEY, TableId int NOT NULL REFERENCES DiningTable(Id), OpenedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), ClosedAt datetime2 NULL);
CREATE UNIQUE INDEX UX_OpenTable ON DiningSession(TableId) WHERE ClosedAt IS NULL;
CREATE TABLE Orders(Id int IDENTITY PRIMARY KEY, Code nvarchar(40) NOT NULL UNIQUE, SessionId int NULL REFERENCES DiningSession(Id), OrderType nvarchar(10) NOT NULL CHECK(OrderType IN ('Counter','Table')), Status nvarchar(20) NOT NULL DEFAULT 'Open', CreatedBy int NOT NULL REFERENCES Users(Id), CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), RequestKey nvarchar(80) NOT NULL UNIQUE, Version rowversion);
CREATE UNIQUE INDEX UX_SessionOrder ON Orders(SessionId) WHERE SessionId IS NOT NULL;
CREATE TABLE OrderDetail(Id int IDENTITY PRIMARY KEY, OrderId int NOT NULL REFERENCES Orders(Id), ProductId int NOT NULL REFERENCES Product(Id), ProductNameSnapshot nvarchar(120) NOT NULL, UnitPriceSnapshot decimal(18,0) NOT NULL CHECK(UnitPriceSnapshot>=0), Quantity int NOT NULL CHECK(Quantity BETWEEN 1 AND 100), KitchenStatus nvarchar(20) NOT NULL DEFAULT 'Draft' CHECK(KitchenStatus IN ('Draft','Waiting','Cooking','Ready','Served','Cancelled')), AddedKey nvarchar(80) NOT NULL, KitchenBatchId nvarchar(80) NULL, SentAt datetime2 NULL, StartedAt datetime2 NULL, ServedAt datetime2 NULL, Version rowversion);
CREATE TABLE Payment(Id int IDENTITY PRIMARY KEY, OrderId int NOT NULL UNIQUE REFERENCES Orders(Id), Method nvarchar(20) NOT NULL CHECK(Method IN ('Cash','Transfer')), Amount decimal(18,0) NOT NULL CHECK(Amount>=0), CashReceived decimal(18,0) NULL, PaidAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), ActorId int NOT NULL REFERENCES Users(Id), RequestKey nvarchar(80) NOT NULL UNIQUE);
CREATE TABLE Ingredient(Id int IDENTITY PRIMARY KEY, Name nvarchar(100) NOT NULL, Unit nvarchar(20) NOT NULL, Stock decimal(18,3) NOT NULL CHECK(Stock>=0), MinStock decimal(18,3) NOT NULL CHECK(MinStock>=0), Version rowversion);
CREATE TABLE Recipe(ProductId int NOT NULL REFERENCES Product(Id), IngredientId int NOT NULL REFERENCES Ingredient(Id), QuantityPerUnit decimal(18,3) NOT NULL CHECK(QuantityPerUnit>0), PRIMARY KEY(ProductId,IngredientId));
CREATE TABLE CancellationRequest(Id int IDENTITY PRIMARY KEY, OrderId int NOT NULL REFERENCES Orders(Id), RequesterId int NOT NULL REFERENCES Users(Id), ReviewerId int NULL REFERENCES Users(Id), Reason nvarchar(500) NOT NULL, RequestedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), ReviewedAt datetime2 NULL, Status nvarchar(20) NOT NULL CHECK(Status IN ('Pending','Approved','Rejected')));
CREATE TABLE InventoryTransaction(Id int IDENTITY PRIMARY KEY, IngredientId int NOT NULL REFERENCES Ingredient(Id), OrderDetailId int NULL REFERENCES OrderDetail(Id), QuantityDelta decimal(18,3) NOT NULL, Type nvarchar(20) NOT NULL CHECK(Type IN ('Opening','Adjustment','Consume')), RecipeAmountSnapshot decimal(18,3) NULL, ActorId int NOT NULL REFERENCES Users(Id), OccurredAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), RequestKey nvarchar(80) NOT NULL, Reason nvarchar(500) NULL, CancellationRequestId int NULL REFERENCES CancellationRequest(Id), WasteQuantity decimal(18,3) NOT NULL DEFAULT 0 CHECK(WasteQuantity>=0));
CREATE UNIQUE INDEX UX_ConsumeOnce ON InventoryTransaction(OrderDetailId,IngredientId) WHERE Type='Consume' AND OrderDetailId IS NOT NULL;
CREATE UNIQUE INDEX UX_InventoryRequest ON InventoryTransaction(RequestKey,IngredientId,Type);
CREATE TABLE OrderStatusHistory(Id int IDENTITY PRIMARY KEY, OrderId int NOT NULL REFERENCES Orders(Id), OrderDetailId int NULL REFERENCES OrderDetail(Id), OldState nvarchar(30) NULL, NewState nvarchar(30) NOT NULL, ActorId int NOT NULL REFERENCES Users(Id), OccurredAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), EventType nvarchar(40) NOT NULL, RequestKey nvarchar(80) NOT NULL);
END;

IF COL_LENGTH(N'dbo.Product', N'ImagePath') IS NULL
    ALTER TABLE Product ADD ImagePath nvarchar(240) NULL;
IF OBJECT_ID(N'dbo.CatalogMigration', N'U') IS NULL
    CREATE TABLE CatalogMigration(Id nvarchar(100) NOT NULL PRIMARY KEY, AppliedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());

IF COL_LENGTH(N'dbo.Product', N'Photo') IS NULL
    ALTER TABLE Product ADD Photo varbinary(max) NULL;
