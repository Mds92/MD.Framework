USE [Smartiz2Real]
GO

/****** Object:  StoredProcedure [dbo].[RunTsql]    Script Date: 3/6/2015 5:39:01 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Mohammad Dayyan>
-- Create date: <1393/12/01>
-- Description:	<mds_soft@yahoo.com, 09197898568>
-- =============================================
CREATE PROCEDURE [dbo].[RunTsql]
	@Tsql NVARCHAR(MAX)
AS
BEGIN
	EXEC (@Tsql);
END

GO


