using System;
using System.Threading.Tasks;
using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Operations.Repositories
{
    [Obsolete]
    public interface IRecoveryTokensRepository
    {
        Task<RecoveryTokenLevel1Entity> GetAccessTokenLvl1Async(string accessTokenLvl1);
        Task RemoveAccessTokenLvl1Async(string accessTokenLvl1);
    }

    public class RecoveryTokensRepository : IRecoveryTokensRepository
    {
        private readonly INoSQLTableStorage<RecoveryTokenLevel1Entity> _recoveryTokenLevel1Entities;

        public RecoveryTokensRepository(INoSQLTableStorage<RecoveryTokenLevel1Entity> recoveryTokenLevel1Entities)
        {
            _recoveryTokenLevel1Entities = recoveryTokenLevel1Entities;
        }

        public async Task<RecoveryTokenLevel1Entity> GetAccessTokenLvl1Async(string accessTokenLvl1)
        {
            var entity = await _recoveryTokenLevel1Entities
                .GetDataAsync(RecoveryTokenLevel1Entity.Partition, accessTokenLvl1);
            return entity;
        }

        public async Task RemoveAccessTokenLvl1Async(string accessTokenLvl1)
        {
            await _recoveryTokenLevel1Entities
                .DeleteIfExistAsync(RecoveryTokenLevel1Entity.Partition, accessTokenLvl1);
        }
    }

    public class RecoveryTokenLevel1Entity : TableEntity
    {
        public const string Partition = "TokenLevel1";

        public RecoveryTokenLevel1Entity() { }
        public RecoveryTokenLevel1Entity(string clientId, string accessToken)
        {
            IssueDateTime = DateTime.Now;
            PartitionKey = Partition;
            RowKey = accessToken;
            ClientId = clientId;
        }

        public string ClientId { get; set; }
        public string AccessToken => RowKey;
        public DateTime IssueDateTime { get; set; }

        public string EmailVerificationCode { get; set; }
        public string PhoneVerificationCode { get; set; }
        public DateTime? VerificationSendDateTime { get; set; }
    }
}
