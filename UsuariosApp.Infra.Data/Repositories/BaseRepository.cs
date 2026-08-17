using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsuarioApp.Domain.Interfaces.Repositories;
using UsuariosApp.Infra.Data.Contexts;

namespace UsuariosApp.Infra.Data.Repositories
{
    public class BaseRepository<TEntity>(DataContext context) : IBaseRepository<TEntity> 
        where TEntity : class
    {
        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            
            
                await context.AddAsync(entity, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            
            
                context.Update(entity);
                await context.SaveChangesAsync(cancellationToken);
            
        }

        public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            
            
                context.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
            
        }

        public Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
           
            
                return context.Set<TEntity>().ToListAsync(cancellationToken);
            
        }

        public async Task<TEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            
            
                return await context.Set<TEntity>().FindAsync([id], cancellationToken);
            
        }
    }
}
