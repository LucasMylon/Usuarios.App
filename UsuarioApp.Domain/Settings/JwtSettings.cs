using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsuarioApp.Domain.Settings
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
    }
}
