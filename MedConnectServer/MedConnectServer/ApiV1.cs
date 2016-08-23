using System;

using Nancy;
using Nancy.ModelBinding;

using MedConnectMongoLib;

namespace MedConnectServer {
    public sealed class ApiV1 : NancyModule {
        public ApiV1()
            : base("/api/v1")
        {
            Get["/hello"] = _ => new {
                    Message = "Hello, world!"
            };

            Get["/doctors", true] = async (p, x) => {
                DoctorInfo[] doctors = await MongoConnection.MongoCtl.FindDoctors();
                return doctors;
            };
        }
    }
}
