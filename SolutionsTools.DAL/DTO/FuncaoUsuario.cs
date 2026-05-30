using Dapper.Contrib.Extensions;

namespace SolutionsTools.DAL
{
    [Table("tb_funcao_usuario")]
    public class FuncaoUsuario
    {
        private int id_funcao;
        private int id_usuario;

        public int Id_Funcao { get => id_funcao; set => id_funcao = value; }
        public int Id_Usuario { get => id_usuario; set => id_usuario = value; }
    }
}
