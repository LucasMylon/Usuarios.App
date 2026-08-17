using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsuarioApp.Domain.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<TEvent>(TEvent evento, CancellationToken cancellationToken = default);
    }
}
