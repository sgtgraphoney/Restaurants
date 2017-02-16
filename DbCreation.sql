CREATE TABLE dbo.Employees (
	Id INT IDENTITY(1, 1) PRIMARY KEY CLUSTERED,
	FirstName NVARCHAR(30) NOT NULL,
	Patronymic NVARCHAR(30) NULL,
	LastName NVARCHAR(30) NOT NULL,
	[Shift] NVARCHAR(10) NULL,
	AmountOfWorkingHours INT NOT NULL,
	[Session] CHAR(3) NOT NULL,
	FirstWorkingDay DATE NOT NULL,

	CHECK ([Shift] IN (N'утренняя', N'вечерняя') OR [Shift] IS NULL),
	CHECK (AmountOfWorkingHours >= 4 AND AmountOfWorkingHours <= 10),
	CHECK ([Session] IN ('2/2', '5/2'))
);
GO


CREATE TABLE dbo.Attestations (
	Id INT IDENTITY(1, 1) PRIMARY KEY CLUSTERED,
	Specialization NVARCHAR(30) NOT NULL UNIQUE
);
GO


CREATE TABLE dbo.EmployeeAttestations (
	Employee_Id INT NOT NULL,
	Attestation_Id INT NOT NULL,

	FOREIGN KEY(Employee_Id)
	REFERENCES dbo.Employees(Id)
	ON UPDATE CASCADE
	ON DELETE CASCADE,

	FOREIGN KEY(Attestation_Id)
	REFERENCES dbo.Attestations(Id)
	ON UPDATE CASCADE
	ON DELETE CASCADE
);
GO


CREATE TABLE dbo.Restaurants (
	Id INT IDENTITY(1, 1) PRIMARY KEY CLUSTERED,
	[Address] VARCHAR(50) NOT NULL
);
GO


CREATE TABLE dbo.Shedule (
	RestaurantId INT NOT NULL,
	[Date] DATE NOT NULL,
	[From] TIME NOT NULL,
	[To] TIME NOT NULL,
	EmployeeId INT NOT NULL

	FOREIGN KEY(RestaurantId)
	REFERENCES dbo.Restaurants(Id)
	ON UPDATE CASCADE
	ON DELETE CASCADE,

	FOREIGN KEY(EmployeeId)
	REFERENCES dbo.Employees(Id)
	ON UPDATE CASCADE
	ON DELETE CASCADE
);
GO


drop table EmployeeAttestations;
drop table Attestations;
drop table Employees;
