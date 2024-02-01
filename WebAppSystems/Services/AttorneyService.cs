using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using WebAppSystems.Data;
using WebAppSystems.Models;
using WebAppSystems.Models.Dto;
using WebAppSystems.Services.Exceptions;

namespace WebAppSystems.Services
{
    public class AttorneyService
    {
        private readonly WebAppSystemsContext _context;

        public AttorneyService(WebAppSystemsContext context)
        {
            _context = context;
        }
        public List<Attorney> FindAll()
        {
            return _context.Attorney.OrderBy(x => x.Name).ToList();
        }

        public async Task<List<Attorney>> FindAllAsync()
        {
            // return await _context.Attorney.ToListAsync();
            var attorneys = await _context.Attorney.ToListAsync();

            foreach (var attorney in attorneys)
            {
                var completeAttorney = await FindByIdAsync(attorney.Id);
                attorney.Department = completeAttorney.Department;
            }

            return attorneys;

        }

        public async Task<List<AttorneyDTO>> GetAllAttorneysAsync()
        {
            var attorneys = await _context.Attorney.ToListAsync();
            return attorneys.Select(a => new AttorneyDTO
            {
                Id = a.Id,
                Name = a.Name,
                DepartmentId = a.DepartmentId,
            }).ToList();
        }


        public Attorney ListarPorId(int id)
        {
            return _context.Attorney.FirstOrDefault(x => x.Id == id);
        }

        public async Task InsertAsync(Attorney obj)
        {
            obj.RegisterDate = DateTime.Now;
            obj.SetSenhaHash();
            _context.Add(obj);
            await _context.SaveChangesAsync();
        }
        public async Task<Attorney> FindByIdAsync(int id)
        {
            return await _context.Attorney.Include(obj => obj.Department).FirstOrDefaultAsync(obj => obj.Id == id);
        }

        public async Task RemoveAsync(int id)
        {
            try
            {
                var obj = await _context.Attorney.FindAsync(id);
                _context.Remove(obj);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new IntegrityException("Não é possível excluir esse usuário, pois ele possui atividades!");
            }

        }

        public async Task UpdateAsync(Attorney obj)
        {
            var hasAny = await _context.Attorney.AnyAsync(x => x.Id == obj.Id);
            if (!hasAny)
            {
                throw new NotFoundException("Id not found");
            }
            try
            {
                obj.UpdateDate = DateTime.Now;

                // Carrega a senha existente do banco de dados
                var existingAttorney = await _context.Attorney.AsNoTracking().FirstOrDefaultAsync(x => x.Id == obj.Id);
                obj.Password = existingAttorney.Password;
                obj.RegisterDate = existingAttorney.RegisterDate; // Mantém o valor original

                _context.Update(obj);
                await _context.SaveChangesAsync();
            }
            catch (DbConcurrencyException ex)
            {
                throw new DbConcurrencyException(ex.Message);
            }
        }

        public Attorney AtualizarSenha(Attorney usuario)
        {
            Attorney usuarioDB = ListarPorId(usuario.Id);

            if (usuarioDB == null) throw new Exception("Houve um erro na atualização do Usuario!");
            usuarioDB.Name = usuario.Name;
            usuarioDB.Email = usuario.Email;
            usuarioDB.Login = usuario.Login;
            usuarioDB.UpdateDate = DateTime.Now;
            usuarioDB.Perfil = usuario.Perfil;

            _context.Attorney.Update(usuarioDB);
            _context.SaveChanges();
            return usuarioDB;

        }
        public Attorney FindByLoginAsync(string login)
        {
            return _context.Attorney.FirstOrDefault(x => x.Login == login);
        }
        public Attorney BuscarPorEmailLogin(string email, string login)
        {
            return _context.Attorney.FirstOrDefault(x => x.Email.ToUpper() == email.ToUpper() && x.Login.ToUpper() == login.ToUpper());
        }

        public Attorney AlterarSenha(AlterarSenhaModel alterarSenhaModel)
        {
            Attorney usuarioDB = ListarPorId(alterarSenhaModel.Id);

            if (usuarioDB == null) throw new Exception("Houve um erro na atualização da senha, usuáro não encontrado");

            if (!usuarioDB.ValidaSenha(alterarSenhaModel.SenhaAtual)) throw new Exception("Senha atual não confere");

            if (usuarioDB.ValidaSenha(alterarSenhaModel.SenhaNova)) throw new Exception("Senha nova deve ser diferente da atual");

            usuarioDB.SetNovaSenha(alterarSenhaModel.SenhaNova);

            _context.Attorney.Update(usuarioDB);

            _context.SaveChanges();

            return usuarioDB;
        }

        public bool IsValidUser(string username, string password)
        {
            try
            {
                Attorney usuario = FindByLoginAsync(username);
                if (usuario != null && usuario.ValidaSenha(password))
                {
                    return true;
                }
                return false;
            }
            catch (Exception erro)
            {
                // Lidar com erros, registrar ou lançar exceções, se necessário.
                return false;
            }
        }

    }
}
