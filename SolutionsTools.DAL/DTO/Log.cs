using Dapper.Contrib.Extensions;
using System;

namespace SolutionsTools.DAL
{
    [Table("TB_Log")]
    public class Log
    {
        private int id;
        private DateTime dt_log;
        private int id_usuario;
        private string entidade;
        private Guid id_cliente;
        private string acao;

        public int Id { get => id; set => id = value; }
        public DateTime Dt_log { get => dt_log; set => dt_log = value; }
        public int Id_Usuario { get => id_usuario; set => id_usuario = value; }
        public string Entidade { get => entidade; set => entidade = value; }
        public Guid Id_Cliente { get => id_cliente; set => id_cliente = value; }
        public string Acao { get => acao; set => acao = value; }
    }

    public enum EntityType
    {
        CL,
        PR
    }
}
