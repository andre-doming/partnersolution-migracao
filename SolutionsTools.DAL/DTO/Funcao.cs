using Dapper.Contrib.Extensions;

namespace SolutionsTools.DAL
{
    [Table("tb_funcao")]
    public class Funcao
    {
        private int id;
        private string cod_funcao;
        private string nome_funcao;
        private string ativo;

        public int Id { get => id; set => id = value; }
        public string Cod_Funcao { get => cod_funcao; set => cod_funcao = value; }
        public string Nome_Funcao { get => nome_funcao; set => nome_funcao = value; }
        public string Ativo { get => ativo; set => ativo = value; }
    }
}
