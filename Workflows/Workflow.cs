using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using WorkflowApi.Core.Models;
using WorkflowApi.Core.Reflection;

namespace WorkflowApi.Core.Workflows
{
	public abstract class Workflow : IWorkflow
	{
		public virtual DbContext DbContext { get; set; }
		public virtual IRequest Request { get; set; }

		public virtual void OnSetup(DbContext dbContext, IRequest request)
		{
			DbContext = dbContext;
			Request = request;
		}

		public virtual void OnAuthorize(IDictionary<string, StringValues> headers)
		{ }

		public abstract object Execute(object entryData);
	}

	public abstract class Workflow<T> : Workflow, IWorkflow<T>
		where T : class
	{
		public override object Execute(object entryData)
		{
			return Execute((T)entryData);
		}

		public abstract T Execute(T entryData);
	}

	public abstract class Workflow<T, S> : Workflow, IWorkflow<T, S>
		where T : class
	{
		public override object Execute(object entryData)
		{
			return Execute((T)entryData);
		}

		public abstract S Execute(T entryData);
	}

	public abstract class QueryableWorkflow<T> : Workflow<T, IQueryable<T>>
		where T : class
	{
		public override object Execute(object entryData)
		{
			return Execute(((IQueryable<T>)entryData).AsQueryable(DbContext));
		}

		public override IQueryable<T> Execute(T entryData)
		{
			return Execute(entryData.AsQueryable(DbContext));
		}

		public abstract IQueryable<T> Execute(IQueryable<T> entryData);
	}

	public abstract class QueryableWorkflow<T, S> : Workflow<T, S>
		where T : class
	{
		public override object Execute(object entryData)
		{
			return Execute(((IQueryable<T>)entryData).AsQueryable(DbContext));
		}

		public override S Execute(T entryData)
		{
			return Execute(entryData.AsQueryable(DbContext));
		}

		public abstract S Execute(IQueryable<T> entryData);
	}

	public interface IWorkflow
	{
		void OnSetup(DbContext dbContext, IRequest request);

		void OnAuthorize(IDictionary<string, StringValues> headers);

		object Execute(object entryData);
	}

	public interface IWorkflow<T> : IWorkflow
	{
		T Execute(T entryData);
	}

	public interface IWorkflow<T, S> : IWorkflow
	{
		S Execute(T entryData);
	}

	public static class IWorkflowExtensions
	{
		public static T As<T>(this object data)
		{
			return (T)data;
		}

		public static IEnumerable<T> AsEnumerable<T>(this object data)
		{
			return (IEnumerable<T>)data;
		}

		public static IQueryable<T> AsQueryable<T>(this object data)
		{
			return (IQueryable<T>)data;
		}

		//public static IQueryable<T> AsQueryable<T>(this object data, DbContext dbContext)
		//	where T : class
		//{
		//	return AsQueryable((T)data, dbContext);
		//}

		public static IQueryable<T> AsQueryable<T>(this T data, DbContext dbContext)
				where T : class
		{
			IQueryable<T> query;

			if (data == default)
			{
				query = dbContext.Set<T>();
			}
			else
			{
				query = data
					.AsQueryable<T>();
			}

			return query;
		}

		public static IQueryable<T> AsQueryable<T>(this IQueryable<T> data, DbContext dbContext)
			where T : class
		{
			if (data == default)
			{
				return dbContext.Set<T>();
			}

			return data;
		}
	}
}
