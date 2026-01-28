using Microsoft.EntityFrameworkCore.Metadata.Conventions;
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
        public void Add(TEntity entity)
        {
            
            
                context.Add(entity);
                context.SaveChanges();
            
        }

        public void Update(TEntity entity)
        {
            
            
                context.Update(entity);
                context.SaveChanges();
            
        }

        public void Delete(TEntity entity)
        {
            
            
                context.Remove(entity);
                context.SaveChanges();
            
        }

        public List<TEntity> GetAll()
        {
           
            
                return context.Set<TEntity>().ToList();
            
        }

        public TEntity? FindById(Guid id)
        {
            
            
                return context.Set<TEntity>().Find(id);
            
        }
    }
}
