using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SftpMonitorService.TransferLog
{
    [Serializable]
    public class Model
    {
        public enum TransferType
        {
            FTP,
            SFTP
        }

        public string Information { get; } = string.Empty;
        public TransferType Type { get; } = TransferType.FTP;
        public DateTime Created { get; }

        public Model(string information, TransferType type, DateTime created)
        {
            Information = information;
            Type = type;
            Created = created;
        }
    }
}
