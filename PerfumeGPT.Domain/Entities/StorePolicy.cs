using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
    public class StorePolicy : BaseEntity<Guid>
    {
        private StorePolicy() { }

        public decimal RequiredDepositPercentage { get; private set; }
        public int DepositTimeoutMinutes { get; private set; }
        public bool IsDepositRequiredForCOD { get; private set; }

        public static StorePolicy Create(decimal percentage, int timeoutMinutes, bool isRequired)
       {
            return Create(Guid.NewGuid(), percentage, timeoutMinutes, isRequired);
        }

        public static StorePolicy Create(Guid id, decimal percentage, int timeoutMinutes, bool isRequired)
        {
         var policy = new StorePolicy
            {
             Id = id
            };
            policy.UpdateDepositPolicy(percentage, timeoutMinutes, isRequired);
            return policy;
        }

        public void UpdateDepositPolicy(decimal percentage, int timeoutMinutes, bool isRequired)
        {
            if (percentage < 0 || percentage > 100)
                throw DomainException.BadRequest("Phần trăm cọc không hợp lệ.");

            if (timeoutMinutes < 0)
                throw DomainException.BadRequest("Thời gian hết hạn cọc không hợp lệ.");

            RequiredDepositPercentage = percentage;
            DepositTimeoutMinutes = timeoutMinutes;
            IsDepositRequiredForCOD = isRequired;
        }
    }
}