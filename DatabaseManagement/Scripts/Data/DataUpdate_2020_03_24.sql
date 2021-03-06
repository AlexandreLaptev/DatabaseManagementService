/*
This script was created by Visual Studio on 3/24/2020 at 10:22 AM.
Run this script on localhost.Northwind (ANASTASIA-PC\alex_) to make it the same as ANASTASIA-PC.Northwind_1001 (ANASTASIA-PC\alex_).
This script performs its actions in the following order:
1. Disable foreign-key constraints.
2. Perform DELETE commands. 
3. Perform UPDATE commands.
4. Perform INSERT commands.
5. Re-enable foreign-key constraints.
Please back up your target database before running this script.
*/
SET NUMERIC_ROUNDABORT OFF
GO
SET XACT_ABORT, ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON
GO
/*Pointer used for text / image updates. This might not be needed, but is declared here just in case*/
DECLARE @pv binary(16)
BEGIN TRANSACTION
ALTER TABLE [dbo].[Order Details] DROP CONSTRAINT [FK_Order_Details_Orders]
ALTER TABLE [dbo].[Order Details] DROP CONSTRAINT [FK_Order_Details_Products]
ALTER TABLE [dbo].[EmployeeTerritories] DROP CONSTRAINT [FK_EmployeeTerritories_Territories]
ALTER TABLE [dbo].[EmployeeTerritories] DROP CONSTRAINT [FK_EmployeeTerritories_Employees]
ALTER TABLE [dbo].[Territories] DROP CONSTRAINT [FK_Territories_Region]
ALTER TABLE [dbo].[Products] DROP CONSTRAINT [FK_Products_Categories]
ALTER TABLE [dbo].[Products] DROP CONSTRAINT [FK_Products_Suppliers]
ALTER TABLE [dbo].[CustomerCustomerDemo] DROP CONSTRAINT [FK_CustomerCustomerDemo]
ALTER TABLE [dbo].[CustomerCustomerDemo] DROP CONSTRAINT [FK_CustomerCustomerDemo_Customers]
ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [FK_Orders_Customers]
ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [FK_Orders_Shippers]
ALTER TABLE [dbo].[Orders] DROP CONSTRAINT [FK_Orders_Employees]
ALTER TABLE [dbo].[Employees] DROP CONSTRAINT [FK_Employees_Employees]
GO
SET IDENTITY_INSERT [dbo].[Employees] ON
INSERT INTO [dbo].[Employees] ([EmployeeID], [LastName], [FirstName], [MiddleName], [Title], [TitleOfCourtesy], [BirthDate], [HireDate], [Address], [City], [Region], [PostalCode], [Country], [HomePhone], [Extension], [Photo], [Notes], [ReportsTo], [PhotoPath]) VALUES (10, N'Gorshkov', N'Peter', N'M', N'Sales Representative', N'Mr.', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL)
SET IDENTITY_INSERT [dbo].[Employees] OFF
GO
SET IDENTITY_INSERT [dbo].[Suppliers] ON
INSERT INTO [dbo].[Suppliers] ([SupplierID], [CompanyName], [ContactName], [ContactTitle], [Address], [City], [Region], [PostalCode], [Country], [Phone], [Fax], [Email], [HomePage]) VALUES (30, N'Red Tree', N'Elena Ivanova', N'Sales Representative', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'Elena.Ivanova@redtree.com', NULL)
INSERT INTO [dbo].[Suppliers] ([SupplierID], [CompanyName], [ContactName], [ContactTitle], [Address], [City], [Region], [PostalCode], [Country], [Phone], [Fax], [Email], [HomePage]) VALUES (31, N'Green Lawn', N'Gregory Peredelko', N'Sales Representative', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'Gregory.Peredelko@greenlawn.com', NULL)
INSERT INTO [dbo].[Suppliers] ([SupplierID], [CompanyName], [ContactName], [ContactTitle], [Address], [City], [Region], [PostalCode], [Country], [Phone], [Fax], [Email], [HomePage]) VALUES (32, N'Blue Sky', N'Irina Sharapova', N'Sales Representative', NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'Irina.Sharapova@bluesky.com', NULL)
SET IDENTITY_INSERT [dbo].[Suppliers] OFF
ALTER TABLE [dbo].[Order Details]
    WITH NOCHECK ADD CONSTRAINT [FK_Order_Details_Orders] FOREIGN KEY ([OrderID]) REFERENCES [dbo].[Orders] ([OrderID])
ALTER TABLE [dbo].[Order Details]
    WITH NOCHECK ADD CONSTRAINT [FK_Order_Details_Products] FOREIGN KEY ([ProductID]) REFERENCES [dbo].[Products] ([ProductID])
ALTER TABLE [dbo].[EmployeeTerritories]
    ADD CONSTRAINT [FK_EmployeeTerritories_Territories] FOREIGN KEY ([TerritoryID]) REFERENCES [dbo].[Territories] ([TerritoryID])
ALTER TABLE [dbo].[EmployeeTerritories]
    ADD CONSTRAINT [FK_EmployeeTerritories_Employees] FOREIGN KEY ([EmployeeID]) REFERENCES [dbo].[Employees] ([EmployeeID])
ALTER TABLE [dbo].[Territories]
    ADD CONSTRAINT [FK_Territories_Region] FOREIGN KEY ([RegionID]) REFERENCES [dbo].[Region] ([RegionID])
ALTER TABLE [dbo].[Products]
    WITH NOCHECK ADD CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryID]) REFERENCES [dbo].[Categories] ([CategoryID])
ALTER TABLE [dbo].[Products]
    ADD CONSTRAINT [FK_Products_Suppliers] FOREIGN KEY ([SupplierID]) REFERENCES [dbo].[Suppliers] ([SupplierID])
ALTER TABLE [dbo].[CustomerCustomerDemo]
    ADD CONSTRAINT [FK_CustomerCustomerDemo] FOREIGN KEY ([CustomerTypeID]) REFERENCES [dbo].[CustomerDemographics] ([CustomerTypeID])
ALTER TABLE [dbo].[CustomerCustomerDemo]
    ADD CONSTRAINT [FK_CustomerCustomerDemo_Customers] FOREIGN KEY ([CustomerID]) REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[Orders]
    WITH NOCHECK ADD CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerID]) REFERENCES [dbo].[Customers] ([CustomerID])
ALTER TABLE [dbo].[Orders]
    WITH NOCHECK ADD CONSTRAINT [FK_Orders_Shippers] FOREIGN KEY ([ShipVia]) REFERENCES [dbo].[Shippers] ([ShipperID])
ALTER TABLE [dbo].[Orders]
    ADD CONSTRAINT [FK_Orders_Employees] FOREIGN KEY ([EmployeeID]) REFERENCES [dbo].[Employees] ([EmployeeID])
ALTER TABLE [dbo].[Employees]
    ADD CONSTRAINT [FK_Employees_Employees] FOREIGN KEY ([ReportsTo]) REFERENCES [dbo].[Employees] ([EmployeeID])
COMMIT TRANSACTION
