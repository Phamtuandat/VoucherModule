namespace SagaService.Sagas
{
    public class UserRegistrationState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string? CurrentState { get; set; }

        public Guid UserId { get; set; }
        public string? Username { get; set; }

        public DateTime RegisteredAt { get; set; }
        public DateTime CreatedAt { get;  set; } = DateTime.UtcNow;
        public string Email { get;  set; } = null!;
    }
}
