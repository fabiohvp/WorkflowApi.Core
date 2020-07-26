using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using WorkflowApi.Core.Models;
using WorkflowApi.Core.Reflection;
using WorkflowApi.Core.Workflows;

namespace WorkflowApi.Core
{
    public class Processor : IProcessor, IObservable<object>
    {
        public virtual Func<DbContext> DbContextFactory { get; protected set; }
        public virtual Func<Exception, object> FormatException { get; set; }
        public virtual List<IObserver<object>> Observers { get; set; }
        public virtual Func<TypeCreator> TypeCreatorFactory { get; protected set; }
        public virtual ConcurrentQueue<Response> Values { get; set; }
        public virtual Func<string, object[], Type, IWorkflow> WorkflowFactory { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="contextFactory">Method that returns a DbContext instance</param>
        /// <param name="workflowFactory">Method that receives the workflow name (string) and arguments (object[]) and must return an instance that implements IWorkflow interface</param>
        public Processor(Func<TypeCreator> typeCreatorFactory, Func<DbContext> dbContextFactory,Func<string, object[], Type, IWorkflow> workflowFactory, Func<Exception, object> formatException = default)
        {
            DbContextFactory = dbContextFactory;
            TypeCreatorFactory = typeCreatorFactory;
            WorkflowFactory = workflowFactory;
            Observers = new List<IObserver<object>>();
            Values = new ConcurrentQueue<Response>();

            FormatException = formatException ?? (ex => ex);
        }

        public virtual void ProcessRequests(IEnumerable<IRequest> requests, IDictionary<string, StringValues> headers)
        {
            Chunk(requests)
                .AsParallel()
                .ForAll(_requests => ProcessRequestsInternal(_requests, headers));

            foreach (var observer in Observers)
            {
                observer.OnCompleted();
            }
        }

        public virtual void ProcessRequestsSync(IEnumerable<IRequest> requests, IDictionary<string, StringValues> headers)
        {
            Chunk(requests)
                .ForEach(_requests => ProcessRequestsInternal(_requests, headers));

            foreach (var observer in Observers)
            {
                observer.OnCompleted();
            }
        }

        public virtual object ProcessRequest(IRequest request, IDictionary<string, StringValues> headers, DbContext dbContext, object data = default)
        {
            var workflow = WorkflowFactory(request.Run, request.Args, data?.GetType());
            workflow.OnSetup(dbContext, request, TypeCreatorFactory());
            workflow.OnAuthorize(headers);
            return workflow.Execute(data);
        }

        public virtual IDisposable Subscribe(IObserver<object> observer)
        {
            if (!Observers.Contains(observer))
            {
                Observers.Add(observer);

                foreach (var value in Values)
                {
                    observer.OnNext(value);
                }
            }

            return new Unsubscriber<object>(Observers, observer);
        }

        private List<IEnumerable<IRequest>> Chunk(IEnumerable<IRequest> items)
        {
            var lists = new List<IEnumerable<IRequest>>();
            var list = new List<IRequest>();

            foreach (var item in items)
            {
                list.Add(item);

                if (!item.Continue)
                {
                    list[list.Count - 1].Evaluate = true;
                    lists.Add(list);
                    list = new List<IRequest>();
                }
            }

            return lists;
        }

        private void Fire(IEnumerable<IRequest> requests, IDictionary<string, StringValues> headers, DbContext dbContext)
        {
            if (requests.Any())
            {
                ProcessChunk(requests, headers, dbContext);
            }
        }

        private void FireAndForget(IEnumerable<IRequest> requests, IDictionary<string, StringValues> headers, DbContext dbContext)
        {
            if (requests.Any())
            {
                Task.Run(() =>
                {
                    var workflowApi = new Processor(TypeCreatorFactory, DbContextFactory, WorkflowFactory, FormatException);
                    workflowApi.Fire(requests, headers, dbContext);
                });
            }
        }

        private void NotifyObservers(object result)
        {
            foreach (var observer in Observers)
            {
                observer.OnNext(result);
            }
        }

        private void ProcessChunk(IEnumerable<IRequest> requests, IDictionary<string, StringValues> headers, DbContext dbContext)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var firstRequest = requests.First();
            var response = new Response { Id = firstRequest.Id };
            try
            {
                var value = ProcessRequest(firstRequest, headers, dbContext);
                requests = requests.Skip(1);

                foreach (var request in requests)
                {
                    value = ProcessRequest(request, headers, dbContext, value);
                }

                var valueType = value.GetType();

                if (value != default && valueType.IsQueryable())
                {
                    value = GetType()
                        .GetMethod(nameof(Evaluate), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        .MakeGenericMethod(valueType.GetGenericArguments().First())
                        .Invoke(null, new object[] { value });
                }

                response.Value = value;
            }
            catch (Exception ex)
            {
                ex.Data.Add("Id", firstRequest.Id);
                response.Error = FormatException(ex);
            }
            finally
            {
                stopwatch.Stop();
                response.Statistics = new { TotalTime = stopwatch.Elapsed };
                Values.Enqueue(response);
                NotifyObservers(response);
            }
        }

        private static object Evaluate<T>(IQueryable<T> query)
        {
            return query.ToList();
        }

        private void ProcessRequestsInternal(IEnumerable<IRequest> chunk, IDictionary<string, StringValues> headers)
        {
            var dbContext = DbContextFactory();

            try
            {
                if (chunk.Any(o => o.FireAndForget))
                {
                    FireAndForget(chunk, headers, dbContext);
                }
                else
                {
                    Fire(chunk, headers, dbContext);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                dbContext.Dispose();
            }
        }
    }

    public interface IProcessor
    {
        Func<Exception, object> FormatException { get; set; }
        Func<TypeCreator> TypeCreatorFactory { get; }
        ConcurrentQueue<Response> Values { get; set; }
        Func<string, object[], Type, IWorkflow> WorkflowFactory { get; }

        object ProcessRequest(IRequest request, IDictionary<string, StringValues> headers, DbContext dbContext, object data = null);
        void ProcessRequests(IEnumerable<IRequest> requests, IDictionary<string, StringValues> headers);
        void ProcessRequestsSync(IEnumerable<IRequest> requests, IDictionary<string, StringValues> headers);
    }
}
