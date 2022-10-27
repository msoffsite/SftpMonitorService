using Microsoft.Extensions.Options;
using Renci.SshNet;
using SftpMonitorService.TransferLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SftpMonitorService.Services
{
    public interface ISftpService
    {
        Task Run(FileSystemEventArgs e);
    }

    public class SftpService : ISftpService
    {
        private readonly ILogger<SftpService> _logger;
        private readonly IOptions<SftpServiceSettings> _sFtpServiceSettings;
        private readonly IHandler _logFileHandler;

        public SftpService(ILogger<SftpService> logger, IOptions<SftpServiceSettings> sFtpServiceSettings, IHandler logFileHandler)
        {
            _logger = logger;
            _sFtpServiceSettings = sFtpServiceSettings;
            _logFileHandler = logFileHandler;
        }

        public async Task Run(FileSystemEventArgs e)
        {
            string host = _sFtpServiceSettings.Value.Host;
            string logMessage = $"Executing SFTP transfer to {host}.";
            _logger.LogInformation(logMessage);
            _logFileHandler.Insert(logMessage);

            using var client = new SftpClient(host, _sFtpServiceSettings.Value.UserName, _sFtpServiceSettings.Value.Password);
            try
            {
                client.Connect();
                using var s = File.OpenRead(e.FullPath);
                await Task.Run(() => client.UploadFile(s, e.Name));

                logMessage = $"SFTP file {e.Name} transferred.";
                _logger.LogInformation(logMessage);
                _logFileHandler.Insert(logMessage);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"An exception has occurred: {ex.Message}");
            }
            finally
            {
                _logger.LogInformation($"Removing {e.Name}.");
                await Task.Run(() => File.Delete(e.FullPath));
                _logger.LogInformation($"{e.Name} removed.");
                _logger.LogInformation($"SFTP transfer to {host} concluded.");
                client.Disconnect();
            }
        }
    }
}
