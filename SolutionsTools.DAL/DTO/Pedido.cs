using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionsTools.DAL.Class
{
    public class Pedido
    {
        private string orderId;
        private int sequence; 
        private string status;
        private DateTime creationDate;
        private Cliente clientProfileData;

        public string OrderId { get => orderId; set => orderId = value; }
        public int Sequence { get => sequence; set => sequence = value; }
        public string Status { get => status; set => status = value; }
        public DateTime CreationDate { get => creationDate; set => creationDate = value; }
        public Cliente ClientProfileData { get => clientProfileData; set => clientProfileData = value; }
    }

    public class ListaPedido 
    {
        [JsonProperty(PropertyName = "list")]
        public List<Pedido> Pedidos;

    }
}
