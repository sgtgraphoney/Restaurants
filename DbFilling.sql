INSERT INTO dbo.Attestations (Specialization)
VALUES (N'Русская кухня'), (N'Итальянская кухня'), (N'Японская кухня');


DECLARE @i INT
SET @i = 1
WHILE @i <= 20
BEGIN 
	INSERT INTO dbo.Restaurants DEFAULT VALUES
	SET @i = @i + 1
END
	

CREATE TABLE #FirstNames (
	FirstName NVARCHAR(30)
);
INSERT INTO #FirstNames
VALUES (N'Иван'), (N'Петр'), (N'Сергей'), (N'Владимир'), (N'Александр'), (N'Дмитрий'),
(N'Аркадий'), (N'Семен'), (N'Виктор'), (N'Валентин'), (N'Антон');

CREATE TABLE #LastNames (
	LastName NVARCHAR(30)
);
INSERT INTO #LastNames
VALUES (N'Иванов'), (N'Петров'), (N'Сергеев'), (N'Попов'), (N'Трамп'), (N'Алексеев'),
(N'Кузнецов'), (N'Семенов'), (N'Рыбаков'), (N'Павлов'), (N'Антонов');

CREATE TABLE #Patronymics (
	Patronymic NVARCHAR(30)
);
INSERT INTO #Patronymics
VALUES (N'Алексеевич'), (N'Андреевич'), (N'Анатольевич'), (N'Константинович'), (N'Витальевич'), 
(N'Александрович'), (N'Юрьевич'), (N'Романович'), (N'Ильич'), (N'Иосифович'), (N'Николаевич');

CREATE TABLE #Sessions (
	[Session] CHAR(3)
);
INSERT INTO #Sessions
VALUES ('2/2'), ('5/2');

CREATE TABLE #Shifts (
	[Shift] NVARCHAR(8)
);
INSERT INTO #Shifts
VALUES (N'утренняя'), (N'вечерняя'), (NULL);

CREATE TABLE #Hours (
	[Hours] INT
);
INSERT INTO #Hours
VALUES (4), (5), (6), (7), (8), (9), (10);

CREATE TABLE #Days (
	[Days] DATE
);
INSERT INTO #Days
VALUES (DATEFROMPARTS(2017, 1, 1));


INSERT INTO dbo.Employees (FirstName, Patronymic, LastName, [Shift], AmountOfWorkingHours,
[Session], FirstWorkingDay)
SELECT TOP(100) *, DATEFROMPARTS(2017, 1, (FLOOR(RAND() * 30 * h.[Hours] / 10)) + 1)
FROM #FirstNames, #Patronymics, #LastNames, #Shifts, #Hours h, #Sessions
ORDER BY ABS(CHECKSUM(NEWID()));


INSERT INTO dbo.EmployeeAttestations (Employee_Id, Attestation_Id)
SELECT e.Id, a.Id
FROM dbo.Employees e, dbo.Attestations a
WHERE a.Id IN 
(SELECT Id
FROM dbo.Attestations
WHERE Id = e.AmountOfWorkingHours % 3 + 1 OR Id = ABS(CHECKSUM(NEWID())) % 4);


UPDATE dbo.Employees
SET [Shift] = NULL
WHERE AmountOfWorkingHours > 7;
