CREATE TABLE TaskQueue (
		TaskId int $$1$$,
		Description varchar(128) not null,
		TaskType int not null,
		TaskDataSerialized varchar(1024) not null
	)
GO
