create table Documents
(
	ID int not null primary key identity(1,1),
	Data varbinary(max),
	Extension char(4),
	FileName Varchar(100)
)
select * from Documents
drop table Documents