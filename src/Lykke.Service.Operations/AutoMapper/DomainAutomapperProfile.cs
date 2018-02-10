using AutoMapper;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;

namespace Lykke.Service.Operations.AutoMapper
{
    internal sealed class DomainAutoMapperProfile : Profile
    {
        public DomainAutoMapperProfile()
        {
            CreateMap<Operation, OperationModel>();
        }
    }
}
