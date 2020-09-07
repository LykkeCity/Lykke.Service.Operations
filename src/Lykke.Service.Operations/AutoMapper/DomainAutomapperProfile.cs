using AutoMapper;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Core.Domain;
using Lykke.Service.Operations.Core.Domain.MyNoSqlEntities;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.AutoMapper
{
    internal sealed class DomainAutoMapperProfile : Profile
    {
        public DomainAutoMapperProfile()
        {
            CreateMap<Operation, OperationModel>()
                .ForMember(d => d.ContextJson, s => s.MapFrom(op => op.Context))
                .ForMember(d => d.Context, s => s.ResolveUsing(op => JObject.Parse(op.Context)));

            CreateMap<OperationEntity, Operation>()
                .ForMember(d => d.OperationValues, s => s.Ignore())
                .ForMember(d => d.OperationValuesJObject, s => s.Ignore());
        }
    }
}
