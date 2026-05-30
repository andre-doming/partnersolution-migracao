using Dapper.Contrib.Extensions;
using Newtonsoft.Json;
using System;

namespace SolutionsTools.DAL
{
    [Table("TB_Empresa")]
    public class Empresa
    {
        private int id;
        private string cnpj;
        private string nome_fantasia;
        private string razao_social;
        private string gerente_responsavel;
        private string ativo;
        private Guid id_parceiro;

        [JsonProperty(PropertyName = "IdEmpresa")]
        public int Id { get => id; set => id = value; }

        [JsonProperty(PropertyName = "CNPJ")]
        public string Cnpj { get => cnpj; set => cnpj = value; }

        [JsonProperty(PropertyName = "NomeFantasia")]
        public string Nome_Fantasia { get => nome_fantasia; set => nome_fantasia = value; }

        [JsonProperty(PropertyName = "RazaoSocial")]
        public string Razao_Social { get => razao_social; set => razao_social = value; }

        [JsonProperty(PropertyName = "Gerente_Responsavel")]
        public string Gerente_Responsavel { get => gerente_responsavel; set => gerente_responsavel = value; }

        [JsonProperty(PropertyName = "Ativo")]
        public string Ativo { get => ativo; set => ativo = value; }

        [JsonProperty(PropertyName = "Id")]
        public Guid Id_Parceiro { get => id_parceiro; set => id_parceiro = value; }
    }
}
