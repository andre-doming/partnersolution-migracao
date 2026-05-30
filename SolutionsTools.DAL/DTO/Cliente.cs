using Dapper.Contrib.Extensions;
using Newtonsoft.Json;
using System;

namespace SolutionsTools.DAL
{
    [Table("TB_Cliente")]
    public class Cliente
    {
        private int id;
        private string nome;
        private string sobrenome;
        private string cpf;
        private string email;
        private string sexo;
        private string dt_nascimento;
        private string empresa;
        private Guid empresaId;
        private string empresaAnterior;
        private string departamento;
        private string cargo;
        private bool aprovado;
        private string ativo;
        private Guid id_cliente;
        private Guid id_parceiro;
        private string acao;
        private int seqId;

        
        [Write(true)]
        public int Id { get => id; set => id = value; }

        [JsonProperty(PropertyName = "firstName")]
        [Write(true)]
        public string Nome { get => nome; set => nome = value; }

        [JsonProperty(PropertyName = "lastName")]
        [Write(true)]
        public string Sobrenome { get => sobrenome; set => sobrenome = value; }

        [JsonProperty(PropertyName = "document")]
        [Write(true)]
        public string Cpf { get => cpf; set => cpf = value; }

        [JsonProperty(PropertyName = "email")]
        [Write(true)]
        public string Email { get => email; set => email = value; }

        [JsonProperty(PropertyName = "gender")]
        [Write(false)]
        public string Sexo { get => sexo; set => sexo = value; }

        [JsonProperty(PropertyName = "birthDate")]
        [Write(false)]
        public string Dt_Nascimento { get => dt_nascimento; set => dt_nascimento = value; }

        [JsonProperty(PropertyName = "company")]
        [Write(false)]
        public string Empresa { get => empresa; set => empresa = value; }

        [JsonProperty(PropertyName = "partnerId")]
        [Write(false)]
        public Guid EmpresaId { get => empresaId; set => empresaId = value; }

        [JsonProperty(PropertyName = "previousCompany")]
        [Write(false)]
        public string EmpresaAnterior { get => empresaAnterior; set => empresaAnterior = value; }

        [JsonProperty(PropertyName = "department")]
        [Write(false)]
        public string Departamento { get => departamento; set => departamento = value; }

        [JsonProperty(PropertyName = "cargo")]
        [Write(false)]
        public string Cargo { get => cargo; set => cargo = value; }

        [JsonProperty(PropertyName = "approved")]
        [Write(false)]
        public bool Aprovado { get => aprovado; set => aprovado = value; }

        [Write(true)]
        public string Ativo { get => ativo; set => ativo = value; }

        [JsonProperty(PropertyName = "id")]
        [Write(true)]
        public Guid Id_Cliente { get => id_cliente; set => id_cliente = value; }

        [Write(true)]
        public Guid Id_Parceiro { get => id_parceiro; set => id_parceiro = value; }

        [Write(false)]
        public string Acao { get => acao; set => acao = value; }

        [Write(false)]
        public int SeqId { get => seqId; set => seqId = value; }

    }
}
