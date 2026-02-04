using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsuariosApp.Infra.Messages.Settings
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string VirtualHost { get; set; }
        public string QueueName { get; set; } = string.Empty;
    }
}
