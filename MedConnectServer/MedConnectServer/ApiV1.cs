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

            Post["/consult", true] = async (p, x) => {
                dynamic args = this.Bind<DynamicDictionary>();
                long tid = args.TelegramId;
                DoctorInfo doctor =  await MongoConnection.MongoCtl.FindSingleDoctor(tid);
                MagicHash magicHash = await MongoConnection.MongoCtl.GenerateAndStoreMagicHashes(doctor);
                return new {
                    MagicHash = magicHash,
                };
            };
        }
    }
}
