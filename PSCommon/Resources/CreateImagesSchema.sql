$$3$$ Patients;
$$3$$ Studies;
$$3$$ Series;
$$3$$ Images;
GO

CREATE TABLE Patients (
		IntPatID int $$1$$,
		PatID varchar(64) not null,
		PatName varchar(64) not null,
		PatBirthDate varchar(8),
		PatSex varchar(16),
		NumStudies int not null,
		NumSeries int not null,
		NumImages int not null,
		PatSizeKB real not null,
		LastUsedTime datetime
		) $$2$$
GO

CREATE UNIQUE INDEX Pat_IntPatID ON Patients(IntPatID);
CREATE UNIQUE INDEX Pat_UniquePat ON Patients(PatID, PatName, PatBirthDate);
CREATE INDEX Pat_PatID ON Patients(PatID);
CREATE INDEX Pat_PatName ON Patients(PatName);
GO

CREATE TABLE Studies (
		IntStuID int $$1$$,
		StuInstID varchar(64) not null,
		IntPatID int not null,
		PatID varchar(64) not null,
		NumSeries int not null,
		NumImages int not null,
		StuSizeKB int not null,
		StuID varchar(16),
		StuDate varchar(8) not null,
		StuTime varchar(16) not null,
		AccessionNum varchar(16),
		Modality varchar(4) not null,
		RefPhysician varchar(64),
		StuDesc varchar(64),
		DeptName varchar(64),
		LastUsedTime datetime
		) $$2$$
GO

CREATE UNIQUE INDEX Stu_IntStuID ON Studies(IntStuID);
CREATE UNIQUE INDEX Stu_StuInstID ON Studies(StuInstID);
CREATE INDEX Stu_IntPatID ON Studies(IntPatID);
CREATE INDEX Stu_StuDate ON Studies(StuDate);
GO

CREATE TABLE Series (
		IntSerID int $$1$$,
		SerInstID varchar(64) not null,
		IntStuID int,
		NumImages int not null,
		SerSizeKB int not null,
		SerDate varchar(8),
		SerTime varchar(16),
		SerNum int not null,
		Modality varchar(2) not null,
		SerDesc varchar(64),
		BodyPart varchar(64),
		LastUsedTime datetime
		) $$2$$
GO

CREATE UNIQUE INDEX Ser_SerInstID ON Series(SerInstID);
CREATE UNIQUE INDEX Ser_IntSerID ON Series(IntSerID);
CREATE INDEX Ser_IntStuID ON Series(IntStuID);
GO

CREATE TABLE Images (
		ImaInstID varchar(64) not null,
		IntSerID int,
		SOPClassID varchar(64) not null,
		TransferSyntaxID varchar(64) not null,
		ImaNum int not null,
		FileSizeKB int not null,
		Path varchar(255) not null,
		SendingAE varchar(16),
		LastUsedTime datetime
		) $$2$$
GO

CREATE UNIQUE INDEX Ima_ImaInstID ON Images(ImaInstID);
CREATE UNIQUE INDEX Ima_Path ON Images(Path);
CREATE INDEX Ima_IntSerID ON Images(IntSerID);
GO
