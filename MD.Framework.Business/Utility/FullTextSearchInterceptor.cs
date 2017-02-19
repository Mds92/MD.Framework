using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Text.RegularExpressions;

namespace MD.Framework.Business
{
	public class FullTextSearchInterceptor : IDbCommandInterceptor
	{
		private const string FullTextPrefix = "-FullTextSearchInterceptor-";
		public static string FullTextSearch(string search)
		{
			return $"({FullTextPrefix}{search})";
		}
		public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
		{
		}
		public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
		{
		}
		public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
		{
			RewriteFullTextQuery(command);
		}
		public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
		{
		}
		public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
		{
			RewriteFullTextQuery(command);
		}
		public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
		{
		}
		public static void RewriteFullTextQuery(DbCommand cmd)
		{
			var text = cmd.CommandText;
			for (var i = 0; i < cmd.Parameters.Count; i++)
			{
				var parameter = cmd.Parameters[i];
				if (!parameter.DbType.In(DbType.String, DbType.AnsiString, DbType.StringFixedLength, DbType.AnsiStringFixedLength))
					continue;
				if (parameter.Value == DBNull.Value)
					continue;
				var value = (string)parameter.Value;
				if (value.IndexOf(FullTextPrefix, StringComparison.InvariantCultureIgnoreCase) < 0) continue;
				parameter.Size = 4096;
				parameter.DbType = DbType.AnsiStringFixedLength;
				value = value.Replace(FullTextPrefix, ""); // remove prefix we added n linq query
				value = value.Substring(1, value.Length - 2); // remove %% escaping by linq translator from string.Contains to sql LIKE
				parameter.Value = value;
				cmd.CommandText = Regex.Replace(text,
				    $@"\[(\w*)\].\[(\w*)\]\s*LIKE\s*@{parameter.ParameterName}\s?(?:ESCAPE N?'~')",
				    $@"contains([$1].[$2], @{parameter.ParameterName})");
				if (text == cmd.CommandText)
					throw new Exception("Full Text Search was not replaced on: " + text);
				text = cmd.CommandText;
			}
		}
	}
}