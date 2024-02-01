namespace WebAppSystems.Models.ViewModels
{
    public class ClientViewModel
    {
        public Client Client { get; set; }
        public ICollection<Client> Clients { get; set; }
    }
}
