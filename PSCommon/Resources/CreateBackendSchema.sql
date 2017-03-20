$$3$$ Entities;
$$3$$ Users;
$$3$$ Logs;
$$3$$ Sessions;
$$3$$ Options;
$$3$$ TaskQueue;
GO

/* Server tables */

CREATE TABLE Entities (
		AE char(16) not null,
		Address varchar(64) not null,
		Port int not null,
		Flags int not null, 
		Comment varchar(64) not null
	)
GO
CREATE INDEX Ent_AE ON Entities(AE);
GO

CREATE TABLE Users (
		Username varchar(16) not null,
		Password varchar(32) not null,
		Realname varchar(32) null,
		Access int not null,
		Remoteip varchar(16) null,
		Lastaction datetime null
	)
GO
CREATE UNIQUE INDEX Use_Username ON Users(Username);
GO

/*root/"pass"*/
INSERT INTO Users (Username, Password, Realname, Access) VALUES ('root', '1A1DC91C907325C69271DDF0C944BC72', 'Root User', 15);
GO

CREATE TABLE Logs (
		Username varchar(16) not null,
		Date timestamp not null,
		Entry varchar(255) not null
	)
GO
CREATE INDEX Log_Username ON Logs(Username);
GO

CREATE TABLE Sessions (
		SessionId varchar(32) not null,
		Username varchar(16) not null
	)
GO
CREATE UNIQUE INDEX Session_SessionId ON Sessions(SessionId);
GO

CREATE TABLE Options (
		KeyName varchar(32) not null,
		Value varchar(256) not null
	)
GO
CREATE UNIQUE INDEX Options_KeyName ON Options(KeyName);
GO
INSERT INTO Options (KeyName, Value) VALUES ('SchemaVersion','3');
GO

CREATE TABLE TaskQueue (
		TaskId int $$1$$,
		Description varchar(128) not null,
		TaskType int not null,
		TaskDataSerialized varchar(1024) not null
	)
GO
