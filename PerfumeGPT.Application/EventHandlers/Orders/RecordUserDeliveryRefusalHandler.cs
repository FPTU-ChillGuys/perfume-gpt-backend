using MediatR;
using PerfumeGPT.Domain.Events;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;

namespace PerfumeGPT.Application.EventHandlers.Orders
{
    public class RecordUserDeliveryRefusalHandler : INotificationHandler<OrderRefusedDomainEvent>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RecordUserDeliveryRefusalHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(OrderRefusedDomainEvent notification, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(notification.UserId);
            if (user == null)
                return;

            user.RecordDeliveryRefusal(DateTime.UtcNow);

            _unitOfWork.Users.Update(user);

            // We shouldn't call SaveChangesAsync here if it runs in the same transaction
            // But MediatR events dispatched via UnitOfWork typically save automatically or within caller transaction
        }
    }
}
