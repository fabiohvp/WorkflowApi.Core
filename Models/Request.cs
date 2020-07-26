namespace WorkflowApi.Core.Models
{
    public class Request : IRequest
    {
        /// <summary>
        /// Request identifier, by default the results will be streamed asynchronously this is how you can identify the query result
        /// If you use the synchronous method this may not be necessary
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Workflow that will run, default is Query`1
        /// </summary>
        public string Run { get; set; }

        public bool Cache { get; set; }
        /// <summary>
        /// This will be passed to the workflow constructor
        /// </summary>
        public object[] Args { get; set; }

        /// <summary>
        /// Will be processed using the last request return
        /// </summary>
        public bool Continue { get; set; }

        /// <summary>
        /// When using Linq-to-SQL will execute .ToList()
        /// </summary>
        public bool Evaluate { get; set; }

        /// <summary>
        /// Will run on a separated thread
        /// </summary>
        public bool FireAndForget { get; set; }

        public Request()
        {
            Cache = false;
            Continue = false;
            Evaluate = true;
            FireAndForget = false;
            Run = "Query`1";
        }
    }

    public interface IRequest
    {
        bool Cache { get; set; }
        object[] Args { get; set; }
        bool Continue { get; set; }
        bool Evaluate { get; set; }
        bool FireAndForget { get; set; }
        string Id { get; set; }
        string Run { get; set; }
    }
}
