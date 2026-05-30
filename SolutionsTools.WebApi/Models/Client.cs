using Newtonsoft.Json;

namespace SolutionsTools.WebApi.Models
{
    public class Client
    {
        private string nome;
        private string sobrenome;
        private string cpf;
        private string cnpj;
        private string email;
        private string departamento;
        private string cargo;

        [JsonProperty(PropertyName = "firstName")]
        public string Nome { get => nome; set => nome = value; }

        [JsonProperty(PropertyName = "lastName")]
        public string Sobrenome { get => sobrenome; set => sobrenome = value; }

        [JsonProperty(PropertyName = "document")]
        public string Cpf { get => cpf; set => cpf = value; }

        [JsonProperty(PropertyName = "partnerDocument")]
        public string Cnpj { get => cnpj; set => cnpj = value; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get => email; set => email = value; }

        [JsonProperty(PropertyName = "department")]
        public string Departamento { get => departamento; set => departamento = value; }

        [JsonProperty(PropertyName = "cargo")]
        public string Cargo { get => cargo; set => cargo = value; }

    }
}