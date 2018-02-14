using AutoMapper;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Contracts;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Client
{
    internal sealed class ClientAutomapperProfile : Profile
    {
        public ClientAutomapperProfile()
        {
            CreateMap<AutorestClient.Models.OperationModel, OperationModel>()
                .ForMember(d => d.Context, s => s.ResolveUsing(o => o.Context == null ? null : JObject.FromObject(o.Context)));
            CreateMap<OperationStatus, AutorestClient.Models.OperationStatus>();
            CreateMap<CreateTransferCommand, AutorestClient.Models.CreateTransferCommand>();
            CreateMap<CreateNewOrderCommand, AutorestClient.Models.CreateNewOrderCommand>();
        }
    }
}
