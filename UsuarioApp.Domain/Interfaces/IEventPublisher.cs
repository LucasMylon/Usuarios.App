using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsuarioApp.Domain.Interfaces
{
    public interface IEventPublisher
    {
        Task Publish<TEvent>(TEvent @evento);
    }
}
