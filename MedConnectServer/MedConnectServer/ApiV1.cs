using System;

using Nancy;
using Nancy.ModelBinding;

namespace MedConnectServer {
    public sealed class ApiV1 : NancyModule {
        public ApiV1()
            : base("/api/v1")
        {
            Get["/hello"] = _ => {
                return new {
                    Message = "Hello, world!"
                };
            };
        }
    }
}
