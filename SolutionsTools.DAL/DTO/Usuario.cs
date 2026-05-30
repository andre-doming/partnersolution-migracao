using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SolutionsTools.DAL
{
    [Table("TB_Usuario")]
    public class Usuario
    {
        private int id;
        private string nome;
        private string login;
        private string senha;
        private string email;
        private string primeiro_acesso;
        private string acesso_token;
        private string admin;
        private string ativo;
        private int id_usuario_cadastro;
        public int Id { get => id; set => id = value; }
        public string Nome { get => nome; set => nome = value; }
        public string Login { get => login; set => login = value; }
        public string Senha { get => senha; set => senha = value; }
        public string Email { get => email; set => email = value; }
        public string Primeiro_Acesso { get => primeiro_acesso; set => primeiro_acesso = value; }
        public string Acesso_Token { get => acesso_token; set => acesso_token = value; }
        public string Admin { get => admin; set => admin = value; }
        public string Ativo { get => ativo; set => ativo = value; }
        public int Id_Usuario_Cadastro { get => id_usuario_cadastro; set => id_usuario_cadastro = value; }
    }
}