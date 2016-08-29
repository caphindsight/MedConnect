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
<<<<<<< HEAD
                dynamic args = this.Bind<DynamicDictionary>();
                long tid = args.TelegramId;
                DoctorInfo doctor =  await MongoConnection.MongoCtl.FindSingleDoctor(tid);
=======
                ConsultRequest request = this.Bind<ConsultRequest>();
                DoctorInfo doctor =  await MongoConnection.MongoCtl.FindSingleDoctor(request.TelegramId);
>>>>>>> 5c4e2d92fbb301388a87bd05dab57b873bcf59d0
                MagicHash magicHash = await MongoConnection.MongoCtl.GenerateAndStoreMagicHashes(doctor);
                return new {
                    MagicHash = magicHash,
                };
            };
        }

        public struct ConsultRequest {
            public long TelegramId;
        }
    }
}
