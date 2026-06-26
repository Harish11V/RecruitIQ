namespace RecruitIQ.Contracts;

public record CreateCompanyRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName,
    string Subdomain);
