using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

public class EscalationTier
{
    
        public Guid Id { get; private set; }
        public Guid EscalationPolicyId { get; private set; }
        public Guid TenantId { get; private set; }
        public int Level { get; private set; }              // 1, 2, 3...
        public int EscalateAfterMinutes { get; private set; } // timeout before next tier

        // Navigation
        public EscalationPolicy Policy { get; private set; } = default!;
        public ICollection<EscalationTierAssignment> Assignments { get; private set; } = [];

        private EscalationTier() { }

        public static EscalationTier Create(
            Guid policyId, Guid tenantId, int level, int escalateAfterMinutes)
        {
            return new EscalationTier
            {
                Id = Guid.NewGuid(),
                EscalationPolicyId = policyId,
                TenantId = tenantId,
                Level = level,
                EscalateAfterMinutes = escalateAfterMinutes
            };
        }
    }
