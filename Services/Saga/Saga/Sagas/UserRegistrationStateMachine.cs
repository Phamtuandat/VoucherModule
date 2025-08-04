
using Contract.VoucherEvents;

namespace SagaService.Sagas
{
    public class UserRegistrationStateMachine : MassTransitStateMachine<UserRegistrationState>
    {
        public State AwaitingVoucher { get; private set; }
        public State Completed { get; private set; }

        public Event<UserRegistered> UserRegisteredEvent { get; private set; }
        public Event<VoucherIssuedEvent> VoucherIssuedEvent { get; private set; } // Added event definition

        public UserRegistrationStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => UserRegisteredEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => VoucherIssuedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));

            Initially(
                When(UserRegisteredEvent)
                    .ThenAsync(async context =>
                    {
                        context.Saga.UserId = context.Message.UserId; // Updated to use 'Saga' instead of 'Instance'
                        context.Saga.Email = context.Message.Email;   // Updated to use 'Message' instead of 'Data'
                        context.Saga.CreatedAt = DateTime.UtcNow;

                        // Call the voucher service (via MassTransit request or publish)
                        await context.Publish(new WelcomeVoucherIssue(context.Message.UserId, "WELCOME10"));
                    })
                    .TransitionTo(AwaitingVoucher)
            );

            During(AwaitingVoucher,
                When(VoucherIssuedEvent) // Fixed by referencing the event property instead of the type
                    .Then(ctx => ctx.Saga.CreatedAt = DateTime.UtcNow)
                    .TransitionTo(Completed)
            );
        }
    }

}
