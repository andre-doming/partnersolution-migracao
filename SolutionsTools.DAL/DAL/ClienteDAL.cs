using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using Dapper;
using System.Linq;
using System;

namespace SolutionsTools.DAL
{
    public class ClienteDAL : DBConn
    {
        public long Insert(Cliente cliente)
        {
            using (var conn = Connection())
            {
                return conn.Insert<Cliente>(cliente);
            }
        }
        public void Update(Cliente cliente)
        {
            using (var conn = Connection())
            {
                conn.Update<Cliente>(cliente);
            }
        }
        public void Delete(Cliente cliente)
        {
            using (var conn = Connection())
            {
                conn.Delete<Cliente>(cliente);
            }
        }
        public Cliente GetById(int id)
        {
            using (var conn = Connection())
            {
                var clientes = conn.Query<Cliente>(string.Format("select * from tb_cliente where Id = '{0}'", id));

                if (clientes.Count() > 0)
                {
                    return clientes.First();
                }

                return null;
            }
        }
        public Cliente GetByIdCliente(Guid id)
        {
            using (var conn = Connection())
            {
                var clientes = conn.Query<Cliente>(string.Format("select * from tb_cliente where Ativo = 'S' and id_cliente = '{0}'", id.ToString()));

                if (clientes.Count() > 0)
                {
                    return clientes.First();
                }

                return null;
            }
        }
        public Cliente GetByCpf(string cpf)
        {
            using (var conn = Connection())
            {
                var clientes = conn.Query<Cliente>(string.Format("select * from tb_cliente where Ativo = 'S' and Cpf = '{0}'", cpf));

                if (clientes.Count() > 0)
                {
                    return clientes.First();
                }

                return null;
            }
        }
        public Cliente GetByEmail(string email)
        {
            using (var conn = Connection())
            {
                var clientes = conn.Query<Cliente>(string.Format("select * from tb_cliente where Ativo = 'S' and Email = '{0}'", email));

                if (clientes.Count() > 0)
                {
                    return clientes.First();
                }

                return null;
            }
        }
        public List<Cliente> GetByCompany(Guid id_Empresa)
        {
            var clientes = new List<Cliente>();

            using (var conn = Connection())
            {
                clientes = conn.Query<Cliente>(string.Format("select * from tb_cliente where Ativo = 'S' and (Id_Parceiro = '{0}' or '00000000-0000-0000-0000-000000000000'='{0}')", id_Empresa.ToString())).ToList();

                return clientes;
            }
        }
        public List<Cliente> GetByCompany(Guid id_Empresa, int QtdePagina, int NumPagina)
        {
            var clientes = new List<Cliente>();

            using (var conn = Connection())
            {
                var query = string.Format("select * from tb_cliente " +
                                          "where Ativo = 'S' and (Id_Parceiro = '{0}' or '00000000-0000-0000-0000-000000000000'='{0}') " +
                                          "order by id " +
                                          "OFFSET (({1} - 1) * {2}) ROWS " +
                                          "FETCH NEXT {2} ROWS ONLY", id_Empresa, NumPagina, QtdePagina);

                clientes = conn.Query<Cliente>(query).ToList();

                return clientes;
            }
        }
        public int GetCountByCompany(Guid id_Empresa)
        {
            using (var conn = Connection())
            {
                var query = string.Format("select count(0) Total from tb_cliente where Ativo = 'S' and (Id_Parceiro = '{0}' or '00000000-0000-0000-0000-000000000000'='{0}')", id_Empresa.ToString());

                var qtdeCliente = conn.QuerySingle<int>(query, new { Id = (int?)null, Name = "Total" });
                
                return qtdeCliente;
            }
        }
        public List<Cliente> GetByCustom(string field, string value)
        {
            var clientes = new List<Cliente>();

            using (var conn = Connection())
            {
                var query = string.Format("select * from tb_cliente where Ativo = 'S' and {0} like '%{1}%'", field, value);

                clientes = conn.Query<Cliente>(query).ToList();

                return clientes;
            }
        }
        public List<Cliente> GetByCompanyAndCustom(Guid id_Empresa, string field, string value)
        {
            var clientes = new List<Cliente>();

            using (var conn = Connection())
            {
                var query = string.Format("select * from Vw_Cliente where (Id_Parceiro = '{0}' or '00000000-0000-0000-0000-000000000000'='{0}') and {1} like '%{2}%'", id_Empresa, field, value);

                clientes = conn.Query<Cliente>(query).ToList();

                return clientes;
            }
        }
        public void UpdateByGuid(Cliente cliente, string cpf_hash)
        {
            using (var conn = Connection())
            {
                var query = "UPDATE TB_Cliente SET " +
                            "  Nome = '{1}' " +
                            " ,Sobrenome = '{2}' " +
                            " ,Cpf = '{3}' " +
                            " ,Email = '{4}' " +
                            "WHERE Id_Cliente = '{0}' ";

                conn.Execute(string.Format(query, cliente.Id_Cliente, cliente.Nome, cliente.Sobrenome, cpf_hash, cliente.Email));
            }
        }
    }
}
