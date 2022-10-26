using Microsoft.Extensions.Options;
using System.Net;

namespace SftpMonitorService.Services
{
    public interface IFtpService
    {
        Task Run(FileSystemEventArgs e);
    }

    public class FtpService : IFtpService
    {
        private readonly ILogger<FtpService> _logger;
        private IOptions<FtpServiceSettings> _ftpServiceSettings;

        public FtpService(ILogger<FtpService> logger, IOptions<FtpServiceSettings> ftpServiceSettings)
        {
            _logger = logger;
            _ftpServiceSettings = ftpServiceSettings;
        }

        public async Task Run(FileSystemEventArgs e)
        {
            _logger.LogInformation("Executing FTP transfer.");

            try
            {
                string ftpPath = $"ftp://{_ftpServiceSettings.Value.Host}{_ftpServiceSettings.Value.RemoteFolder}/{e.Name}";
                var username = _ftpServiceSettings.Value.UserName;
                var password = _ftpServiceSettings.Value.Password;
                using var client = new WebClient();
                client.Credentials = new NetworkCredential(username, password);
                await client.UploadFileTaskAsync(ftpPath, WebRequestMethods.Ftp.UploadFile, e.FullPath);
                _logger.LogInformation("FTP file transferred.");
            }
            catch(Exception ex)
            {
                _logger.LogInformation($"An exception has occurred: {ex.Message}");
            }
            finally
            {
                _logger.LogInformation($"Removing {e.Name}.");
                await Task.Run(() => File.Delete(e.FullPath));
                _logger.LogInformation($"{e.Name} removed.");
                _logger.LogInformation("FTP transfer concluded.");
            }
        }
    }
}
