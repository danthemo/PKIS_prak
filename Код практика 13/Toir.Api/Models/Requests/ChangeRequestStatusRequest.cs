namespace Toir.Api.Models.Requests;

public sealed record ChangeRequestStatusRequest(string Status, string ChangedBy);
