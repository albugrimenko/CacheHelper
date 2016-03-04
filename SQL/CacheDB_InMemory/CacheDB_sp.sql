USE [CacheDB] 
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Object_Del]
	@ObjType varchar(100), @ObjKey varchar(36)

--
--
--

AS
set nocount on;
set xact_abort on;

declare @objID int

BEGIN TRY
	--set transaction isolation level SERIALIZABLE;
	BEGIN TRAN
		
		select @objID = ID from ObjectInfo where ObjType = @ObjType and ObjKey = @ObjKey
		if @objID is not null begin
			delete ObjectBody where ObjID = @objID
			delete ObjectInfo where ObjType = @ObjType and ObjKey = @ObjKey --and ObjID = @objID
		end

	COMMIT TRAN
END TRY
BEGIN CATCH
    declare @errSeverity int,
            @errMsg nvarchar(2048)
    select  @errSeverity = ERROR_SEVERITY(),
            @errMsg = ERROR_MESSAGE()

    -- Test XACT_STATE:
        -- If 1, the transaction is committable.
        -- If -1, the transaction is uncommittable and should be rolled back.
        -- XACT_STATE = 0 means that there is no transaction and a commit or rollback operation would generate an error.
    if (xact_state() = 1 or xact_state() = -1)
          ROLLBACK TRAN
      
    raiserror('%u:: %s', 16, 1, @errSeverity, @errMsg)
      
END CATCH

RETURN 1
GO
GRANT EXECUTE ON [dbo].[Object_Del] TO [public] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Object_DeleteExpired]
	@IsCompleteReset bit = 0

--
-- when @IsCompleteReset = 1, it deletes ALL records and RESETS identity column
--

/*
exec Object_DeleteExpired @IsCompleteReset=1
*/

AS
set nocount on;
set xact_abort on;

declare @d datetime = getdate()

BEGIN TRY
	
	if @IsCompleteReset = 1 begin
		BEGIN TRAN
			truncate table ObjectBody
			truncate table ObjectInfo
		COMMIT TRAN
	end else begin
		declare @t as table(ObjID int not null)
		insert into @t (ObjID)
		select ID
		from ObjectInfo (nolock)
		where Expires < @d

		BEGIN TRAN

			delete ObjectBody where ObjID in (select ObjID from @t)
			delete ObjectInfo where ID in (select ObjID from @t)

		COMMIT TRAN
	end

END TRY
BEGIN CATCH
    declare @errSeverity int,
            @errMsg nvarchar(2048)
    select  @errSeverity = ERROR_SEVERITY(),
            @errMsg = ERROR_MESSAGE()

    -- Test XACT_STATE:
        -- If 1, the transaction is committable.
        -- If -1, the transaction is uncommittable and should be rolled back.
        -- XACT_STATE = 0 means that there is no transaction and a commit or rollback operation would generate an error.
    if (xact_state() = 1 or xact_state() = -1)
          ROLLBACK TRAN
      
    raiserror('%u:: %s', 16, 1, @errSeverity, @errMsg)
      
END CATCH

RETURN 1
GO
GRANT EXECUTE ON [dbo].[Object_DeleteExpired] TO [public] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Object_Get]
	@ObjType varchar(100), @ObjKey varchar(36)

--
--
--

AS
set nocount on;

declare @objID int, 
	@dExp datetime,
	@d datetime = getdate(),
	@tInc int = 5	-- sliding expiration window in minutes

select @objID = ID, @dExp = Expires from ObjectInfo where ObjType = @ObjType and ObjKey = @ObjKey
if @objID is not null begin
	if @dExp < @d begin
		delete ObjectBody where ObjID = @objID
		delete ObjectInfo where ObjType = @ObjType and ObjKey = @ObjKey
	end else begin
		select 
			ExpDate = @dExp,
			ObjBody = ObjBody
		from ObjectBody
		where ObjID = @objID

		-- sliding expiration
		if dateadd(minute, @tInc, @dExp) > @d begin
			exec Object_ResetExpiration @ObjType=@ObjType, @ObjKey=@ObjKey, @TTL_min=@tInc
		end
	end
end

RETURN 1
GO
GRANT EXECUTE ON [dbo].[Object_Get] TO [public] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Object_GetCurrentStat]

AS
set nocount on

select 
	i.ObjType,
	Cnt = count(*),
	SizeMin = min(datalength(b.ObjBody)),
	SizeMax = max(datalength(b.ObjBody)),
	SizeAvg = avg(datalength(b.ObjBody)),
	ExpTimeMin = min(i.Expires),
	ExpTimeMax = max(i.Expires)
from ObjectInfo i 
	left join ObjectBody b on i.ID = b.ObjID
group by i.ObjType
order by i.ObjType

RETURN 1
GO
GRANT EXECUTE ON [dbo].[Object_GetCurrentStat] TO [public] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Object_Put]
	@ObjType varchar(100), @ObjKey varchar(36),
	@TTL_min int, 
	@ObjBody varbinary(max)

--
-- if object already exists - its body will be replaced and expiration will slide forward
--
--

/*
exec Object_Put @ObjType = 'test', @ObjKey = 'T_01', @TTL_min = 1, @ObjBody = 0x0012345
*/

AS
set nocount on;
set xact_abort on;

declare @objID int,
		@d datetime = getdate()
declare @dExp datetime = dateadd(minute, @TTL_min, @d)

BEGIN TRY
	--set transaction isolation level SNAPSHOT;
	BEGIN TRAN
		
		update ObjectInfo set @objID = ID, Expires = @dExp where ObjType = @ObjType and ObjKey = @ObjKey
		if @@rowcount = 0 begin
			-- new record
			insert into ObjectInfo (ObjType, ObjKey, Created, Expires)
			values(@ObjType, @ObjKey, @d, @dExp)
			set @objID = scope_identity()

			if (@objID is not null) begin
				insert into ObjectBody (ObjID, ObjBody)
				values (@objID, @ObjBody)
			end
		end else if @objID is not null begin
			update ObjectBody set ObjBody = @ObjBody where ObjID = @objID
		end

	COMMIT TRAN
END TRY
BEGIN CATCH
    declare @errSeverity int,
            @errMsg nvarchar(2048)
    select  @errSeverity = ERROR_SEVERITY(),
            @errMsg = ERROR_MESSAGE()

    -- Test XACT_STATE:
        -- If 1, the transaction is committable.
        -- If -1, the transaction is uncommittable and should be rolled back.
        -- XACT_STATE = 0 means that there is no transaction and a commit or rollback operation would generate an error.
    if (xact_state() = 1 or xact_state() = -1)
          ROLLBACK TRAN
      
    raiserror('%u:: %s', 16, 1, @errSeverity, @errMsg)
      
END CATCH

RETURN 1
GO
GRANT EXECUTE ON [dbo].[Object_Put] TO [public] AS [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Object_ResetExpiration]
	@ObjType varchar(100), @ObjKey varchar(36),
	@TTL_min int

--
-- if object already exists - its expiration will be reset
--
--

/*
exec Object_ResetExpiration @ObjType = 'test', @ObjKey = 'T_01', @TTL_min = 1
*/

AS
set nocount on;
set xact_abort on;

declare @d datetime = getdate()
declare @dExp datetime = dateadd(minute, @TTL_min, @d)

BEGIN TRY
	--set transaction isolation level SERIALIZABLE;
	--BEGIN TRAN
		
		update ObjectInfo set Expires = @dExp where ObjType = @ObjType and ObjKey = @ObjKey

	--COMMIT TRAN
END TRY
BEGIN CATCH
    declare @errSeverity int,
            @errMsg nvarchar(2048)
    select  @errSeverity = ERROR_SEVERITY(),
            @errMsg = ERROR_MESSAGE()

    -- Test XACT_STATE:
        -- If 1, the transaction is committable.
        -- If -1, the transaction is uncommittable and should be rolled back.
        -- XACT_STATE = 0 means that there is no transaction and a commit or rollback operation would generate an error.
    if (xact_state() = 1 or xact_state() = -1)
          ROLLBACK TRAN
      
    raiserror('%u:: %s', 16, 1, @errSeverity, @errMsg)
      
END CATCH

RETURN 1
GO
GRANT EXECUTE ON [dbo].[Object_ResetExpiration] TO [public] AS [dbo]
GO
