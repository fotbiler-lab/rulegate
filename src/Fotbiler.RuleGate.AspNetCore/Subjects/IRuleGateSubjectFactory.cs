using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Subjects;

public interface IRuleGateSubjectFactory
{
    AuthorizationSubject Create(
        ClaimsPrincipal principal);
}
