using Dapper.Contrib.Extensions;

namespace SolutionsTools.DAL
{
    [Table("tb_empresa_usuario")]
    public class EmpresaUsuario
    {
        private int id_empresa;
        private int id_usuario;

        public int Id_Empresa { get => id_empresa; set => id_empresa = value; }
        public int Id_Usuario { get => id_usuario; set => id_usuario = value; }
    }
}
