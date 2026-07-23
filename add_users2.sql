SET IDENTITY_INSERT Sys_User ON;
INSERT Sys_User(Id,UserName,RealName,PasswordHash,PasswordSalt,IsActive,MustChangePwd,IsDeleted,CreateTime,CreateBy) VALUES(2,'zhangwei',N'张伟','AQAAAAIAAYagAAAAECLiRzMfJPj7UwmS6Crt4CGts4Jo4FnS7TEOm5rWMV4l5qdBEDJePvtcxRT77BMZ0g==','',1,1,0,'2026-07-17 08:00:00',N'admin');
INSERT Sys_User(Id,UserName,RealName,PasswordHash,PasswordSalt,IsActive,MustChangePwd,IsDeleted,CreateTime,CreateBy) VALUES(3,'likun',N'李坤','AQAAAAIAAYagAAAAECLiRzMfJPj7UwmS6Crt4CGts4Jo4FnS7TEOm5rWMV4l5qdBEDJePvtcxRT77BMZ0g==','',1,1,0,'2026-07-17 08:00:00',N'admin');
INSERT Sys_User(Id,UserName,RealName,PasswordHash,PasswordSalt,IsActive,MustChangePwd,IsDeleted,CreateTime,CreateBy) VALUES(4,'wangqiang',N'王强','AQAAAAIAAYagAAAAECLiRzMfJPj7UwmS6Crt4CGts4Jo4FnS7TEOm5rWMV4l5qdBEDJePvtcxRT77BMZ0g==','',1,1,0,'2026-07-17 08:00:00',N'admin');
INSERT Sys_User(Id,UserName,RealName,PasswordHash,PasswordSalt,IsActive,MustChangePwd,IsDeleted,CreateTime,CreateBy) VALUES(5,'liujie',N'刘杰','AQAAAAIAAYagAAAAECLiRzMfJPj7UwmS6Crt4CGts4Jo4FnS7TEOm5rWMV4l5qdBEDJePvtcxRT77BMZ0g==','',1,1,0,'2026-07-17 08:00:00',N'admin');
INSERT Sys_User(Id,UserName,RealName,PasswordHash,PasswordSalt,IsActive,MustChangePwd,IsDeleted,CreateTime,CreateBy) VALUES(6,'zhaomin',N'赵敏','AQAAAAIAAYagAAAAECLiRzMfJPj7UwmS6Crt4CGts4Jo4FnS7TEOm5rWMV4l5qdBEDJePvtcxRT77BMZ0g==','',1,1,0,'2026-07-17 08:00:00',N'admin');
INSERT Sys_User(Id,UserName,RealName,PasswordHash,PasswordSalt,IsActive,MustChangePwd,IsDeleted,CreateTime,CreateBy) VALUES(7,'chenhui',N'陈辉','AQAAAAIAAYagAAAAECLiRzMfJPj7UwmS6Crt4CGts4Jo4FnS7TEOm5rWMV4l5qdBEDJePvtcxRT77BMZ0g==','',1,1,0,'2026-07-17 08:00:00',N'admin');
INSERT Sys_User(Id,UserName,RealName,PasswordHash,PasswordSalt,IsActive,MustChangePwd,IsDeleted,CreateTime,CreateBy) VALUES(8,'maxiang',N'马翔','AQAAAAIAAYagAAAAECLiRzMfJPj7UwmS6Crt4CGts4Jo4FnS7TEOm5rWMV4l5qdBEDJePvtcxRT77BMZ0g==','',1,1,0,'2026-07-17 08:00:00',N'admin');
INSERT Sys_User(Id,UserName,RealName,PasswordHash,PasswordSalt,IsActive,MustChangePwd,IsDeleted,CreateTime,CreateBy) VALUES(9,'wangfang',N'王芳','AQAAAAIAAYagAAAAECLiRzMfJPj7UwmS6Crt4CGts4Jo4FnS7TEOm5rWMV4l5qdBEDJePvtcxRT77BMZ0g==','',1,1,0,'2026-07-17 08:00:00',N'admin');
SET IDENTITY_INSERT Sys_User OFF;

INSERT Sys_UserRole(UserId,RoleId) VALUES(2,3);
INSERT Sys_UserRole(UserId,RoleId) VALUES(3,3);
INSERT Sys_UserRole(UserId,RoleId) VALUES(4,2);
INSERT Sys_UserRole(UserId,RoleId) VALUES(5,4);
INSERT Sys_UserRole(UserId,RoleId) VALUES(6,4);
INSERT Sys_UserRole(UserId,RoleId) VALUES(7,4);
INSERT Sys_UserRole(UserId,RoleId) VALUES(8,5);
INSERT Sys_UserRole(UserId,RoleId) VALUES(9,3);
