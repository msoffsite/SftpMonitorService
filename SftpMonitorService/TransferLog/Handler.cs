using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SftpMonitorService.TransferLog
{
    public interface IHandler
    {
        Task<List<Model>> GetRecords();
        void Insert(string information);
    }

    public class Handler : IHandler
    {
        private readonly string LogFile;
        private readonly ILogger<Worker> _logger;

        public Handler(ILogger<Worker> logger)
        {
            var buildDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = buildDir + @"\TransferLog.json";
            LogFile = filePath;
            _logger = logger;
        }

        public async Task<List<Model>> GetRecords()
        {
            var output = new List<Model>();
            string content = await File.ReadAllTextAsync(LogFile);
            if (content.Length > 0)
            {
                output = JsonConvert.DeserializeObject<List<Model>>(content);
            }
            return output ?? new List<Model>();
        }

        public async void Insert(string information)
        {
            try
            {
                var records = new List<Model>();

                using (var fs = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    var logData = await sr.ReadToEndAsync();
                    if (logData.Length > 0)
                    {
                        records = JsonConvert.DeserializeObject<List<Model>>(logData);
                    }
                    logData = null;
                    records ??= new List<Model>();
                    var input = new Model(information, Model.TransferType.SFTP, DateTime.Now);
                    records.Add(input);
                }

                File.WriteAllText(LogFile, JsonConvert.SerializeObject(records));

            }
            catch(Exception ex)
            {
                _logger.LogInformation($"Record insertion for json data file failed. Error Message: {ex.Message}");
            }
        }
    }
}
