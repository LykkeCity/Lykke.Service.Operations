using AutoMapper;
using Lykke.Contracts.Operations;
using Lykke.Service.Operations.Contracts;

namespace Lykke.Service.Operations.Client
{
    public class ClientAutomapperProfile : Profile
    {
        public ClientAutomapperProfile()
        {
            CreateMap<AutorestClient.Models.OperationModel, OperationModel>();
            CreateMap<OperationStatus, AutorestClient.Models.OperationStatus>();
            CreateMap<CreateTransferCommand, AutorestClient.Models.CreateTransferCommand>();
        }
    }
}
