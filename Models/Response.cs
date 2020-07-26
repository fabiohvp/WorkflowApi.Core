using System;

namespace WorkflowApi.Core.Models
{
    public class Response
    {
        public string Id { get; set; }
        public object Value { get; set; }
        public object Error { get; set; }
        public object Statistics { get; set; }
    }
}
