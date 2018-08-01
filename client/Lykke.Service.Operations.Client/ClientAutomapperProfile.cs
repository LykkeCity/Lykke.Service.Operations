using AutoMapper;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;
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
            CreateMap<CreateSwiftCashoutCommand, AutorestClient.Models.CreateSwiftCashoutCommand>();
            CreateMap<CreateCashoutCommand, AutorestClient.Models.CreateCashoutCommand>();
        }
    }
}
