namespace CompanyOps.Domain.Requests;

/// <summary>
/// How an approval step matches its approver. <see cref="Department"/> means the
/// approver must belong to the request's owning department (the dept-scoped Manager
/// invariant from ADR 0005); <see cref="Global"/> means any holder of the step's
/// role may decide it (e.g. central Finance).
/// </summary>
public enum ApprovalScope
{
    Department = 0,
    Global = 1,
}
