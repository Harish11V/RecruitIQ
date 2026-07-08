namespace RecruitIQ.Infrastructure.Services;

public class FileStorageOptions
{
    public const string SectionName = "FileStorageSettings";
    public string UploadFolder { get; set; } = "wwwroot/uploads/logos";
}
