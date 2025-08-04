using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract.Abstracts
{
    public abstract record IntegrationEvent
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
        public string EventType => GetType().AssemblyQualifiedName ?? nameof(IntegrationEvent);
    }
}
